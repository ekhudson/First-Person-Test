using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Whilefun.FPEKit
{

    //
    // FPECore
    // This script allows for simplified handling of game start by ensuring all the required prefabs are created 
    // and placed in your scene when the game starts. It is recommended that the 'FPECore' object 
    // (e.g. FPECore.prebab) be placed in the 1st scene of your game (perhaps the main menu scene). If the first 
    // scene that contained FPECOre is reloaded, you will see a warning printed due to a duplicate instance. You 
    // can ignore this error or comment it out if you know you were loading your first scene again.
    //
    // Copyright 2017 While Fun Games
    // http://whilefun.com
    //
    public class FPECore : MonoBehaviour
    {

        private static FPECore _instance;
        public static FPECore Instance {
            get { return _instance; }
        }

        [SerializeField, Tooltip("")]
        private GameObject eventSystemPrefab;

        [SerializeField, Tooltip("")]
        private GameObject interactionManagerPrefab;

        [SerializeField, Tooltip("")]
        private GameObject playerPrefab;

        [SerializeField, Tooltip("")]
        private GameObject inputManagerPrefab;

        [SerializeField, Tooltip("")]
        private GameObject saveLoadManagerPrefab;

        [SerializeField, Tooltip("")]
        private GameObject menuPrefab;

        private bool initialized = false;
        
        void Awake()
        {

            if (_instance != null)
            {

                // This warning is very harmless, and can be ignored. It should indicate that you are returning to the 
                // first level in your game. If you get this warning in OTHER scenes, it means you have an FPECore
                // prefab in that scene, which you don't need. You can also delete the Debug.Log call if you want.
                Debug.Log("FPECore:: Duplicate FPECore, deleting duplicate instance. If you saw this message when loading a saved game into your 1st scene, ignore. Otherwise, it's still not a problem, but might indicate redundant FPECore in a secondary Scene file or something similar.");

                Destroy(this.gameObject);

            }
            else
            {

                _instance = this;
                DontDestroyOnLoad(this.gameObject);

                if (!initialized)
                {
                    initialized = true;
                    initialize();
                }

            }

        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }


        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void initialize()
        {

            if (!eventSystemPrefab || !interactionManagerPrefab || !playerPrefab || !inputManagerPrefab || !saveLoadManagerPrefab || !menuPrefab)
            {
                Debug.LogError("FPECore:: Missing prefab for core component. Game will not function correctly. See Inspector for object '" + gameObject.name + "' to ensure all fields are populated correctly.");
            }

            Instantiate(eventSystemPrefab, null);
            Instantiate(interactionManagerPrefab, null);

            GameObject player = Instantiate(playerPrefab, null);
            FPEPlayerStartLocation startLocation = GameObject.FindObjectOfType<FPEPlayerStartLocation>();

            if (startLocation != null)
            {
                player.transform.position = startLocation.gameObject.transform.position;
                player.transform.rotation = startLocation.gameObject.transform.rotation;
            }
            else
            {
                Debug.LogWarning("FPECore:: No FPEPlayerStartLocation was found. Placing player at origin");
                player.transform.position = Vector3.zero;
                player.transform.rotation = Quaternion.identity;
            }

            Instantiate(inputManagerPrefab, null);
            Instantiate(saveLoadManagerPrefab, null);
            Instantiate(menuPrefab, null);

            FPEInteractionManagerScript.Instance.initialize();

            // Lastly, load game options
            FPESaveLoadManager.Instance.LoadGameOptions();

        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {

            // If we're in the main menu scene, freeze the player
            if (SceneManager.GetActiveScene().buildIndex == FPESaveLoadManager.Instance.MainMenuSceneBuildIndex)
            {
                FPEInteractionManagerScript.Instance.suspendPlayerAndInteraction();
            }

        }
      
    }

}