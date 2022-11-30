using UnityEngine;

namespace Ui {
    public class Menu : MonoBehaviour {
        public MenuCursor cursor;
        public int selectedItemIndex;

        private MenuItem[] menuItems;

        public MenuItem SelectedItem => menuItems[selectedItemIndex];
        private static int MinIndex => 0;
        private int MaxIndex => menuItems.Length - 1;

        private float timeSinceInput = Mathf.Infinity;

        private void Start() {
            InputManager.SetMode(InputManager.Mode.Interface);

            menuItems = GetComponentsInChildren<MenuItem>();
        }

        private void Update() {
            timeSinceInput += Time.deltaTime;

            if (timeSinceInput < 0.1f) {
                return;
            }

            if (InputManager.InterfaceInput.moveUp && selectedItemIndex > MinIndex) {
                selectedItemIndex--;
                timeSinceInput = 0;
            } else if (InputManager.InterfaceInput.moveDown && selectedItemIndex < MaxIndex) {
                selectedItemIndex++;
                timeSinceInput = 0;
            }

            if (InputManager.InterfaceInput.interact) {
                SelectedItem.Interact();
                timeSinceInput = 0;
            }

            cursor.follow = SelectedItem.transform;
        }

        public void SelectItem(MenuItem item) {
            for (int i = 0; i < menuItems.Length; i++) {
                if (menuItems[i] == item) {
                    selectedItemIndex = i;
                    break;
                }
            }
        }
    }
}