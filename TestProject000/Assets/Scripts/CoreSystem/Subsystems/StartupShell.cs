using UnityEngine;

namespace CoreSystem {

    public class CoreSystem_StartupShell : MonoBehaviour {
        void Awake() {
            STSHELL_SetActive(false);
        }

        public void STSHELL_SetActive(bool state) {
            gameObject.SetActive(state);
        }
    }

}