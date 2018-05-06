using UnityEngine;

namespace Whilefun.FPEKit
{

    //
    // FPESimpleDoor
    // This script is the basis for a simple sliding door type object 
    // that can be configured as part of an Activate-Type Toggle interaction.
    //
    // Copyright 2017 While Fun Games
    // http://whilefun.com
    //
    public class FPESimpleDoor : MonoBehaviour
    {

        public enum eDoorState
        {
            CLOSED = 0,
            CLOSING = 1,
            OPENING = 2,
            OPEN = 3
        }
        private eDoorState currentDoorState = eDoorState.CLOSED;

        private Vector3 openedPosition = Vector3.zero;
        private Vector3 closedPosition = Vector3.zero;
        private FPEInteractableActivateScript myDoorHandle = null;

        [Header("Moving Parts Configuration")]
        [SerializeField, Tooltip("How far the sliding part of the door slides.")]
        private float slidingDistance = 2.0f;
        [SerializeField, Tooltip("How quickly the door opens.")]
        private float openSpeed = 6.0f;
        [SerializeField, Tooltip("How quickly the door closes.")]
        private float closeSpeed = 4.0f;
        [SerializeField, Tooltip("The distance at which the door will snap to open position when opening.")]
        private float openSnapDistance = 0.01f;
        [SerializeField, Tooltip("The distance at which the door will snap to closed position when closing.")]
        private float closeSnapDistance = 0.1f;

        [SerializeField, Tooltip("The sliding part of the door.")]
        private GameObject slidingPart = null;
        [SerializeField, Tooltip("This object blocks the player's path when the door is opening, closing, or fully closed. Should have a primitive collider attached (e.g. a basic cube will work fine).")]
        public GameObject playerBlocker = null;
        [SerializeField, Tooltip("This optional object blocks the raycast from player to door handle. It prevents player from closing door on themselves from inside the doorway.")]
        public GameObject playerInteractionBlocker = null;

        [Header("Sound")]
        [SerializeField, Tooltip("Sound plays when door is opened")]
        private AudioClip doorOpen;
        [SerializeField, Tooltip("Sound plays when door is closed")]
        private AudioClip doorClose;
        [SerializeField, Tooltip("Sound plays when door is locked and someone tries to open it")]
        private AudioClip doorLocked;

        private AudioSource myAudio;

        [Header("Interaction Text")]
        [SerializeField]
        private string openText = "Open Door";
        [SerializeField]
        private string closeText = "Close Door";
        [SerializeField]
        private string lockedText = "It's Locked";

        void Awake()
        {

            if (!slidingPart || !playerBlocker)
            {
                Debug.Log("Door cannot find one of sliding part or player blocker!");
            }

            closedPosition = slidingPart.transform.position;
            openedPosition = closedPosition + slidingPart.transform.right * slidingDistance;

            myAudio = gameObject.GetComponent<AudioSource>();

            if (!myAudio)
            {
                myAudio = gameObject.AddComponent<AudioSource>();
            }

            myAudio.loop = false;
            myAudio.playOnAwake = false;

            // Ensure the door has a door handle
            FPEInteractableActivateScript[] childActivates = gameObject.GetComponentsInChildren<FPEInteractableActivateScript>();

            for (int a = 0; a < childActivates.Length; a++)
            {

                if (childActivates[a].gameObject.name == "DoorHandle")
                {
                    myDoorHandle = childActivates[a];
                    break;
                }

            }

            if (!myDoorHandle)
            {
                Debug.LogError("FPESimpleDoor:: No child Activate Type object called 'DoorHandle' was found on door '" + gameObject.name + "'. Ensure the door has a handle, otherwise it can't be opened!");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }

        }

        void Update()
        {

            if (currentDoorState == eDoorState.CLOSED)
            {
                // Do nothing
            }
            else if (currentDoorState == eDoorState.CLOSING)
            {

                slidingPart.transform.position = Vector3.Lerp(slidingPart.transform.position, closedPosition, closeSpeed * Time.deltaTime);

                if (Vector3.Distance(slidingPart.transform.position, closedPosition) < closeSnapDistance)
                {
                    setDoorClosed();
                }

            }
            else if (currentDoorState == eDoorState.OPENING)
            {

                slidingPart.transform.position = Vector3.Lerp(slidingPart.transform.position, openedPosition, openSpeed * Time.deltaTime);

                if (Vector3.Distance(slidingPart.transform.position, openedPosition) < openSnapDistance)
                {
                    setDoorOpen();
                }

            }
            else if (currentDoorState == eDoorState.OPEN)
            {
                // Do nothing
            }

        }

        // Assign to the Shared 'Activation' event in the inspector
        public void activateDoor()
        {

            if (currentDoorState == eDoorState.CLOSED || currentDoorState == eDoorState.CLOSING)
            {
                openTheDoor();
            }
            else if (currentDoorState == eDoorState.OPEN || currentDoorState == eDoorState.OPENING)
            {
                closeTheDoor();
            }

        }

        private void openTheDoor()
        {

            currentDoorState = eDoorState.OPENING;
            playerBlocker.SetActive(false);

            if (playerInteractionBlocker)
            {
                playerInteractionBlocker.SetActive(true);
            }

            myAudio.PlayOneShot(doorOpen);
            myDoorHandle.setInteractionString(closeText);

        }

        private void closeTheDoor()
        {

            currentDoorState = eDoorState.CLOSING;
            playerBlocker.SetActive(true);

            if (playerInteractionBlocker)
            {
                playerInteractionBlocker.SetActive(true);
            }

            myAudio.PlayOneShot(doorClose);
            myDoorHandle.setInteractionString(openText);

        }

        // Assign to Activation Failure event in the inspector
        public void doorOpenFailure()
        {

            currentDoorState = eDoorState.CLOSED;
            myAudio.PlayOneShot(doorLocked);
            myDoorHandle.setInteractionString(lockedText);

        }

        private void setDoorClosed()
        {

            slidingPart.transform.position = closedPosition;
            currentDoorState = eDoorState.CLOSED;

            if (playerInteractionBlocker)
            {
                playerInteractionBlocker.SetActive(false);
            }

        }

        private void setDoorOpen()
        {

            slidingPart.transform.position = openedPosition;
            currentDoorState = eDoorState.OPEN;
            playerBlocker.SetActive(false);

        }

        public FPEDoorSaveData getSaveGameData()
        {
            return new FPEDoorSaveData(gameObject.name, currentDoorState, myDoorHandle.interactionString);
        }

        public void restoreSaveGameData(FPEDoorSaveData data)
        {

            currentDoorState = data.DoorState;
            myDoorHandle.setInteractionString(data.DoorHandleString);

            switch (currentDoorState)
            {

                case eDoorState.CLOSING:
                case eDoorState.CLOSED:
                    setDoorClosed();
                    break;

                case eDoorState.OPENING:
                case eDoorState.OPEN:
                    setDoorOpen();
                    break;

                default:
                    Debug.LogError("FPESimpleDoor.restoreSaveGameData():: Given bad door state '"+ currentDoorState + "'");
                    break;

            }
            
        }

    }

}