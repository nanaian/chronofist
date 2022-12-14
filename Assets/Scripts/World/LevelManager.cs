using System.Collections.Generic;
using Cinemachine;
using Combat;
using General;
using LDtkUnity;
using UnityEngine;

namespace World {
    /// <summary>
    ///     Loads/unloads levels and controls the camera based on the LevelActivator's position.
    /// </summary>
    public class LevelManager : Singleton<LevelManager> {
        public Camera mainCamera;
        public GameObject world;
        public GameObject follow; // Probably the player
        private readonly List<LoadedLevel> _loadedLevels = new();
        private LoadedLevel _currentLevel;
        [SerializeField] private GameObject camera;

        private LDtkComponentProject _project;

        public static GameObject GetPlayer() {
            return Instance.follow;
        }

        private void Update() {
            // Make the level where 'follow' object is the current level.
            if (follow != null) {
                var level = getLoadedLevelAt(new Vector2(follow.transform.position.x, follow.transform.position.y));
                if (level != null) {
                    makeLevelCurrent(level);
                }
            }
        }

        protected override void init() {
            _project = world.GetComponent<LDtkComponentProject>();
            if (_project == null) {
                Debug.LogError("World must be an LDtk project!");
                return;
            }

            LDtkIidBank.CacheIidData(_project.FromJson()); // Needed for LoadLevelNeighbours

            // For each child of the world, turn it into a LoadedLevel.
            foreach (Transform childTransform in world.transform) {
                var child = childTransform.gameObject;

                var level = child.GetComponent<LDtkComponentLevel>();
                if (level == null) {
                    Debug.LogError($"World child '{child.name}' is not an LDtk level.");
                    continue;
                }

                var bounds = child.GetComponent<PolygonCollider2D>();
                if (bounds == null) {
                    Debug.LogError(
                        $"Level '{child.name}' is missing a PolygonCollider2D - ensure 'Use Composite Collider' is enabled on the World asset.");
                }

                var id = child.GetComponent<LDtkIid>();
                _loadedLevels.Add(new LoadedLevel(child, level, id, bounds, follow.transform,camera));
            }
        }

        private LoadedLevel getLoadedLevel(GameObject gameObject) {
            foreach (var level in _loadedLevels)
                if (level.gameObject == gameObject) {
                    return level;
                }

            return null;
        }

        private LoadedLevel getLoadedLevel(LDtkIid id) {
            foreach (var level in _loadedLevels)
                if (level.id.Iid == id.Iid) {
                    return level;
                }

            return null;
        }

        private LoadedLevel getLoadedLevelAt(Vector2 position) {
            foreach (var level in _loadedLevels)
                if (level.bounds.OverlapPoint(position)) {
                    return level;
                }

            return null;
        }

        private void makeLevelCurrent(LoadedLevel enteredLevel) {
            if (_currentLevel == enteredLevel || enteredLevel == null) {
                return;
            }

            if (_currentLevel != null) {
                _currentLevel.Exit();
            }

            _currentLevel = enteredLevel;
            enteredLevel.Enter();

            // PERF: we could do this asynchronously
            loadLevelNeighbours(enteredLevel.level.Identifier);
            unloadDistantLevels(enteredLevel);
        }

        /// <summary>
        ///     Destroys all loaded levels that are not the given level or its neighbours.
        /// </summary>
        private void unloadDistantLevels(LoadedLevel enteredLevel) {
            // We are going to perform mark-and-sweep garbage collection.

            // 1. Mark all levels for unload.
            foreach (var level in Instance._loadedLevels) level.MarkForUnload = true;

            // 2. Unmark `enteredLevel`.
            enteredLevel.MarkForUnload = false;

            // 3. Unmark the neighbours of `enteredLevel`.
            foreach (var id in enteredLevel.level.Neighbours) {
                if (id == null) {
                    Debug.LogError(
                        $"Level '{enteredLevel.level.Identifier}' has unloaded neighbour(s). Aborting garbage collection.");
                    return;
                }

                var neighbour = getLoadedLevel(id);
                if (neighbour != null) {
                    neighbour.MarkForUnload = false;
                } else

                    // What the fuck?
                {
                    Debug.LogError(
                        $"Level '{enteredLevel.level.Identifier}' has a neighbour '{id.Iid}' that is in the scene but the LevelManager doesn't know about it.");
                }
            }

            // 4. Unload all levels that are still marked.
            foreach (var level in _loadedLevels)
                if (level.MarkForUnload) {
                    Debug.Log($"Unloading level: {level.level.Identifier}");
                    Destroy(level.gameObject);

                    // TODO: Resources.UnloadAsset
                }

            // 5. Remove all levels we unloaded from the list of loaded levels.
            _loadedLevels.RemoveAll(level => level.MarkForUnload);
        }

        private void loadLevelNeighbours(string levelIdentifier) {
            var levels = _project.FromJson().Levels;

            foreach (var level in levels)
                if (level.Identifier == levelIdentifier) {
                    foreach (var neighbour in level.Neighbours) loadLevel(neighbour.Level);
                    break;
                }
        }

        private void loadLevel(Level level) {
            // Check if the level is already loaded.
            foreach (var loadedLevel in _loadedLevels)
                if (loadedLevel.level.Identifier == level.Identifier) {
                    return;
                }

            // Load the level.
            // PERF: consider LoadAsync
            Debug.Log($"Loading level: {level.Identifier}");
            var path = level.ExternalRelPath.Replace(".ldtkl", "");
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) {
                Debug.LogError($"Failed to load resource: {path}");
                return;
            }

            var levelObject = Instantiate(prefab, world.transform); // FIXME: doesnt appear to be adding to the scene
            var levelComponent = levelObject.GetComponent<LDtkComponentLevel>();
            var id = levelObject.GetComponent<LDtkIid>();
            var bounds = levelObject.GetComponent<PolygonCollider2D>();
            _loadedLevels.Add(new LoadedLevel(levelObject, levelComponent, id, bounds, follow.transform,camera));
        }

        private class LoadedLevel {
            private const float SCREEN_WIDTH_TILES = 480f / 8f;
            private const float SCREEN_HEIGHT_TILES = 270f / 8f;
            public readonly PolygonCollider2D bounds;

            public readonly GameObject gameObject;
            public readonly LDtkIid id;
            public readonly LDtkComponentLevel level;
            public readonly GameObject vcamObject;

            public bool MarkForUnload;

            private GameObject entityLayer;

            public LoadedLevel(GameObject gameObject, LDtkComponentLevel level, LDtkIid id, PolygonCollider2D bounds,
                Transform follow,GameObject camera) {
                this.gameObject = gameObject;
                this.level = level;
                this.bounds = bounds;
                this.id = id;

                // The bounds needs to be on the LevelBounds layer.
                bounds.gameObject.layer = LayerMask.NameToLayer("LevelBounds");
                bounds.isTrigger = true;

                // The children of the level need to be on the Level layer.
                // Except for entities- they need to be on the Enemy layer.
                foreach (Transform childTransform in level.gameObject.transform) {
                    var isEntityLayer = childTransform.gameObject.name == "Entities";
                    var layerName = isEntityLayer ? "Enemy" : "Level";
                    childTransform.gameObject.SetLayerRecursively(LayerMask.NameToLayer(layerName));

                    if (isEntityLayer)
                        entityLayer = childTransform.gameObject;
                }

                // Create a child object to hold the virtual camera.
                vcamObject = Instantiate(camera, gameObject.transform);

                // Center it on the level.
                vcamObject.transform.position = new Vector3(bounds.bounds.center.x, bounds.bounds.center.y, -10f);

                // Deactivate it by default (we will enable it when a LevelActivator enters).
                vcamObject.SetActive(false);

                // Set up the actual vcam component.
                var vcam = vcamObject.GetComponent<CinemachineVirtualCamera>();
                vcam.m_Lens.OrthographicSize = CustomCinemachinePixelPerfect.ORTHO_SIZE;
                if (!useStaticCamera()) {
                    vcam.Follow = follow;

                    vcamObject.AddComponent<CustomCinemachinePixelPerfect>();

                    var framingTransposer = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
                    framingTransposer.m_LookaheadTime = 0.6f;
                    framingTransposer.m_LookaheadSmoothing = 5.0f;
                    framingTransposer.m_LookaheadIgnoreY = true;
                    framingTransposer.m_ScreenX = 0.5f;
                    framingTransposer.m_ScreenY = 0.5f;
                    framingTransposer.m_CameraDistance = 10f;

                    //framingTransposer.m_DeadZoneWidth = 0.2f;
                    //framingTransposer.m_DeadZoneHeight = 0.2f;
                    framingTransposer.m_YDamping = 1.0f;

                    var confiner = vcamObject.AddComponent<CinemachineConfiner2D>();
                    confiner.m_BoundingShape2D = bounds;
                }

                SetEntitiesActive(false);
            }

            public void Enter() {
                vcamObject.SetActive(true);
                ScreenShakeReference.SetCurrentCamera(vcamObject.GetComponent<CinemachineVirtualCamera>());
                SetEntitiesActive(true);
            }

            public void Exit() {
                vcamObject.SetActive(false);
                SetEntitiesActive(false);
            }

            private bool useStaticCamera() {
                Debug.Log($"Level {level.Identifier} is {level.BorderRect.width}x{level.BorderRect.height} tiles");
                return level.BorderRect.width - SCREEN_WIDTH_TILES < 0.1f &&
                       level.BorderRect.height - SCREEN_HEIGHT_TILES < 0.1f;
            }

            private void SetEntitiesActive(bool active) {
                if (entityLayer == null) return;
                entityLayer.SetActive(active);
            }
        }
    }
}
