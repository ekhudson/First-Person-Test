using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Whilefun.FPEKit
{

    //
    // FPEInteractionManagerScript
    // This script handles all player actions with respect to Interactable objects in the 
    // game world. All raycast hit detection, input handling, object manipulation, etc. occurs
    // in this script. All interfaces from other scripts to the Player Controller are done
    // through this script as well.
    //
    // Copyright 2017 While Fun Games
    // http://whilefun.com
    //
    public class FPEInteractionManagerScript : MonoBehaviour
    {

        private static FPEInteractionManagerScript _instance;
        public static FPEInteractionManagerScript Instance {
            get { return _instance; }
        }

        [Header("Examination Options")]
        [Tooltip("When examining an object, it will only rotate if axis input is greater than this deadzone value. Default is 0.1.")]
        public float examinationDeadzone = 0.1f;
        [Tooltip("When examining an object, this value acts as a multiplier to Input Mouse X/Y values. Default is 5.8.")]
        public float examineRotationSpeed = 5.8f;
        [Header("Reticle")]
        [Tooltip("Uncheck this to disable reticle completely")]
        public bool reticleEnabled = true;
        [Tooltip("Uncheck this to disable Interaction Text completely")]
        public bool interactionTextEnabled = true;
        [Tooltip("Reticle sprite when it is inactive")]
        public Sprite inactiveReticle;
        [Tooltip("Reticle sprite when it is active")]
        public Sprite activeReticle;
        [Tooltip("Reticle sprite when action is not permitted or unavailable")]
        public Sprite unavailableReticle;
        [Header("Custom Journal Cursor. Note: Only works when using Unity 5+")]
        public Texture2D journalCursor;

        [Header("Special Interaction Masks")]
        [Tooltip("This should be assigned to FPEPutbackObjects layer")]
        public LayerMask putbackLayerMask;
        [Tooltip("This should be assigned to be mixed to include everything except the FPEPutbackObjects and FPEIgnore layers")]
        public LayerMask interactionLayerMask;

        // Max range you can interact with an object. Note that interactions are governed ultimately by the Interactable Object's interactionDistance.
        private float interactionRange = Mathf.Infinity;
        private RectTransform interactionLabel;
        private Vector3 interactionLabelTargetScale = Vector3.zero;
        private Vector3 interactionLabelLargestScale = Vector3.zero;
        private Vector3 interactionLabelSmallestScale = Vector3.zero;
        private RectTransform reticle;
        // Journal stuff
        private GameObject journalCloseIndicator;
        private GameObject journalPreviousButton;
        private GameObject journalNextButton;
        private GameObject journalBackground;
        private GameObject journalPage;
        private Sprite[] currentJournalPages = null;
        private int currentJournalPageIndex = 0;
        //Audio diary stuff
        private RectTransform audioDiaryLabel;
        private Vector3 audioDiaryLabelTargetScale = Vector3.zero;
        private Vector3 audioDiaryLabelLargestScale = Vector3.zero;
        private Vector3 audioDiaryLabelSmallestScale = Vector3.zero;
        private RectTransform audioDiarySkipHintLabel;
        private bool playingAudioDiary = false;
        private GameObject currentAudioDiary = null;
        private bool fadingDiaryText = false;
        private Color defaultDiaryColor;
        private Color diaryFadeColor;
        // Object Interaction Stuff
        private GameObject currentInteractableObject = null;
        private GameObject currentHeldObject = null;
        private GameObject currentPutbackObject = null;
        private GameObject interactionObjectPickupLocation = null;
        private GameObject interactionObjectExamineLocation = null;
        private GameObject interactionObjectTossLocation = null;
        private GameObject interactionInventoryLocation = null;
        private GameObject audioDiaryPlayer = null;
        private GameObject secondaryInteractionSoundPlayer = null;
        private GameObject journalSFXPlayer = null;
        private GameObject genericSFXPlayer = null;
        // Notification Stuff
        private RectTransform notificationLabel;
        private Vector3 notificationLabelTargetScale = Vector3.zero;
        private Vector3 notificationLabelLargestScale = Vector3.zero;
        private Vector3 notificationLabelSmallestScale = Vector3.zero;
        private bool fadingNotificationText = true;
        private Color defaultNotificationColor;
        private Color notificationFadeColor;
        private float notificationCounter = 0.0f;
        private float notificationDuration = 2.5f;

        private bool examiningObject = false;

        // Camera zoom and mouse stuff
        [Header("Mouse Zoom and Sensitivity Options")]
        [Tooltip("This is the FOV the camera will use when player Right-Clicks to zoom in. Un-zoomed FOV is set to be that of Main Camera when scene starts. If you change FOV in Main Camera, also change it in ExaminationCamera.")]
        public float zoomedFOV = 24.0f;
        private float unZoomedFOV = 0.0f;
        private bool cameraZoomedIn = false;
        private float cameraZoomChangeRate = 0.1f;
        [Tooltip("Zoomed mouse sensitivity multiplier (e.g. 0.5 would be half as fast)")]
        public float zoomedMouseSensitivityMultiplier = 0.5f;
        private Vector2 zoomedMouseSensitivity = new Vector2(1.0f, 1.0f);
        [Tooltip("Apply special mouse sensitivity when reticle is over an interactable object")]
        public bool slowMouseOnInteractableObjectHighlight = true;
        [Tooltip("Mouse sensitivity when reticle is over an interactable object (0.5 would be half as sensitive)")]
        public float highlightedMouseSensitivityMultiplier = 0.5f;
        private Vector2 highlightedMouseSensitivity = new Vector2(1.0f, 1.0f);
        private Vector2 startingMouseSensitivity = Vector2.zero;
        // Used to ensure smooth sensitivity changes when mouse is slowed on reticle highlight of object vs. unhighlighted
        private Vector2 targetMouseSensitivity = Vector2.zero;
        private bool smoothMouseChange = false;
        private float smoothMouseChangeRate = 0.5f;
        private GameObject thePlayer = null;
        private bool mouseLookEnabled = true;

        // Examination stuff
        Quaternion lastObjectHeldRotation = Quaternion.identity;
        // This is multiplied with tossStrength of held object. Seems to be an okay value.
        private float tossImpulseFactor = 2.5f;

        // Journal stuff
        [Header("The sounds journals make when used. Use 2D sounds for best effect.")]
        public AudioClip journalOpen;
        public AudioClip journalClose;
        public AudioClip journalPageTurn;
        private GameObject currentJournal = null;

        // Dock stuff
        private GameObject currentDock = null;

        [Header("Other generic sound effects. Use 2D sounds for best effect.")]
        public AudioClip inventoryPickup;
        public AudioClip noteAdded;

        // UI Hint options
        [Header("Control Hints UI")]
        [Tooltip("If true, control hints will be shown in the UI to help player perform actions.")]
        public bool showMouseControlHints = true;
        [Tooltip("Text hint for Pick Up action")]
        public string mouseHintPickUpText = "Pick Up";
        [Tooltip("Text hint for Pick Up object when hands are full")]
        public string mouseHintPickUpHandsFullText = "(There's something already in your hand)";
        [Tooltip("Text hint for Put Back action")]
        public string mouseHintPutBackText = "Put Back";
        [Tooltip("Text hint for Examine action")]
        public string mouseHintExamineText = "Examine";
        [Tooltip("Text hint for Drop action")]
        public string mouseHintDropText = "Drop";
        [Tooltip("Text hint for Zoom In action")]
        public string mouseHintZoomText = "Zoom In";
        [Tooltip("Text hint for Activate action")]
        public string mouseHintActivateText = "Activate";
        [Tooltip("Text hint for telling player they need 2 hands to activate an object")]
        public string activateTwoHandsHint = "(You need both hands free for this)";
        [Tooltip("Text hint for Journal Read action")]
        public string mouseHintJournalText = "Read";
        [Tooltip("Text hint for telling player they need 2 hands to read a journal")]
        public string journalTwoHandsHint = "(You need both hands to read this)";
        [Tooltip("Text hint for Inventory 'Pre' action-from-world text (e.g. Take [x] ")]
        public string inventoryHintFromWorldPreText = "Take";
        [Tooltip("Text hint for Inventory 'Post' action-from-hand text (e.g. PUT [x] [postText]")]
        public string inventoryHintFromHandPreText = "Store";
        [Tooltip("Text hint for Inventory 'Post' action-from-hand text (e.g. [preText] [x] AWAY")]
        public string inventoryHintFromHandPostText = "In Inventory";

        [Tooltip("Text hint for Audio Diary manual playback")]
        public string audioDiaryHint = "Play";
        [Tooltip("UI label for prefix audio diary title (e.g. PLAYING [diaryTitle] [postText]")]
        public string audioDiaryPlayingPreText = "Playing";
        [Tooltip("UI label for prefix audio diary title replay from inventory (e.g. RE-PLAYING [diaryTitle] [postText]")]
        public string audioDiaryReplayPreText = "Re-playing";
        [Tooltip("UI label for postfix audio diary title (e.g. [preText] [diaryTitle] [postText]")]
        public string audioDiaryPlayingPostText = "";

        private FPEUIHint zoomExamineHint = null;
        private FPEUIHint interactHint = null;
        private FPEUIHint inventoryHint = null;
        private FPEUIHint unDockHint = null;

        // Audio Diary stuff
        [Tooltip("Volume fade out amount per 100ms (0.0 to 1.0, with 1.0 being 100% of the volume)")]
        public float fadeAmountPerTenthSecond = 0.1f;
        private bool fadingDiaryAudio = false;
        private float fadeCounter = 0.0f;
        private float notificationColorLerpFactor = 1.8f;
        private float notificationScaleLerpFactor = 0.5f;

        // Menu stuff
        private bool menuOpen = false;
        // Docking stuff
        private bool dockingInProgress = false;
        private FPEFirstPersonController.ePlayerDockingState currentDockActionType = FPEFirstPersonController.ePlayerDockingState.IDLE;
        // Save/Load stuff
        private bool playerSuspendedForSaveLoad = false;
        public bool PlayerSuspendedForSaveLoad { get { return playerSuspendedForSaveLoad; } }

        private FPEInventoryManagerScript inventoryManager = null;
        private bool initialized = false;

        void Awake()
        {

            if (_instance != null)
            {
                Debug.LogWarning("FPEInteractionManager:: Duplicate instance of FPEInteractionManager, deleting second one.");
                Destroy(this.gameObject);
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(this.gameObject);
            }

        }

        void Start()
        {
            initialize();
        }

        public void initialize()
        {

            if (!initialized)
            {

                initialized = true;

                inventoryManager = gameObject.GetComponent<FPEInventoryManagerScript>();
                if (!inventoryManager)
                {
                    Debug.LogError("FPEInteractionManager:: There was no FPEInventoryManagerScript attached to '" + gameObject.name + "'. Did you change the FPEInteractionManager prefab?");
                }

                reticle = transform.Find("Reticle").GetComponent<RectTransform>();
                interactionLabel = transform.Find("InteractionTextLabel").GetComponent<RectTransform>();

                journalBackground = transform.Find("JournalBackground").gameObject;
                journalCloseIndicator = transform.Find("JournalBackground/CloseIndicator").gameObject;
                journalPreviousButton = transform.Find("JournalBackground/PreviousButton").gameObject;
                journalNextButton = transform.Find("JournalBackground/NextButton").gameObject;
                journalPage = transform.Find("JournalBackground/JournalPage").gameObject;
                audioDiaryLabel = transform.Find("AudioDiaryTitleLabel").GetComponent<RectTransform>();
                audioDiarySkipHintLabel = transform.Find("AudioDiarySkipHintLabel").GetComponent<RectTransform>();
                notificationLabel = transform.Find("NotificationLabel").GetComponent<RectTransform>();
                zoomExamineHint = transform.Find("ZoomExamineHint").GetComponent<FPEUIHint>();
                interactHint = transform.Find("InteractHint").GetComponent<FPEUIHint>();
                inventoryHint = transform.Find("InventoryHint").GetComponent<FPEUIHint>();
                unDockHint = transform.Find("UndockHint").GetComponent<FPEUIHint>();

                // UI component error checks
                if (!zoomExamineHint || !interactHint || !inventoryHint || !unDockHint)
                {
                    Debug.LogError("FPEInteractionManagerScript:: UI Hints are missing. Did you change the FPEInteractionManager prefab?");
                }

                // Just in case they were disabled during an edit to the prefab
                zoomExamineHint.gameObject.SetActive(true);
                interactHint.gameObject.SetActive(true);
                inventoryHint.gameObject.SetActive(true);
                unDockHint.gameObject.SetActive(true);

                // The undock hint is sort of special, as we don't necessarily set it like the others. So disable it here to start.
                unDockHint.setHint("");

                if (!reticle || !interactionLabel || !audioDiaryLabel || !audioDiarySkipHintLabel || !notificationLabel || !journalCloseIndicator || !journalPreviousButton || !journalNextButton || !journalBackground || !journalPage)
                {
                    Debug.LogError("FPEInteractionManagerScript:: UI and/or Journal Components are missing. Did you change the FPEInteractionManager prefab?");
                }

                if (!reticleEnabled)
                {
                    reticle.GetComponentInChildren<Image>().enabled = false;
                }

                if (!interactionTextEnabled)
                {
                    interactionLabel.GetComponentInChildren<Text>().enabled = false;
                }

                thePlayer = FPEPlayer.Instance.gameObject;

                if (!thePlayer)
                {
                    Debug.LogError("FPEInteractionManagerScript:: Player could not be found!");
                }

                interactionObjectPickupLocation = thePlayer.transform.Find("MainCamera/ObjectPickupLocation").gameObject;
                interactionObjectExamineLocation = thePlayer.transform.Find("MainCamera/ObjectExamineLocation").gameObject;
                interactionObjectTossLocation = thePlayer.transform.Find("MainCamera/ObjectTossLocation").gameObject;
                interactionInventoryLocation = thePlayer.transform.Find("MainCamera/ObjectInInventoryPosition").gameObject;
                audioDiaryPlayer = thePlayer.transform.Find("FPEAudioDiaryPlayer").gameObject;
                secondaryInteractionSoundPlayer = thePlayer.transform.Find("FPESecondaryInteractionSoundPlayer").gameObject;
                journalSFXPlayer = thePlayer.transform.Find("FPEJournalSFX").gameObject;
                genericSFXPlayer = thePlayer.transform.Find("FPEGenericSFX").gameObject;

                // We don't need the debug meshes at runtime
                Destroy(interactionObjectPickupLocation.GetComponentInChildren<MeshRenderer>().gameObject);
                Destroy(interactionObjectExamineLocation.GetComponentInChildren<MeshRenderer>().gameObject);
                Destroy(interactionObjectTossLocation.GetComponentInChildren<MeshRenderer>().gameObject);
                Destroy(interactionInventoryLocation.GetComponentInChildren<MeshRenderer>().gameObject);

                if (!interactionObjectPickupLocation || !interactionObjectExamineLocation || !interactionObjectTossLocation || !interactionInventoryLocation)
                {
                    Debug.LogError("FPEInteractionManagerScript:: Player or its components are missing. Did you change the Player Controller prefab, or forget to tag with 'Player' tag?");
                }

                if (!audioDiaryPlayer || !secondaryInteractionSoundPlayer || !journalSFXPlayer || !genericSFXPlayer)
                {
                    Debug.LogError("FPEInteractionManagerScript:: FPEAudioDiaryPlayer, FPESecondaryInteractionSoundPlayer, or FPEJournalSFX are missing from Player Controller. Did you break the FPEPlayerController prefab or forget to add one or both of these prefabs to your player controller?");
                }

                // The core system expects there to be some kind of menu present. To work around this, you can simply change the openMenu() and 
                // closeMenu() functions to suit your needs, or make them blank functions, remove them, etc. as required.
                if (FPEMenu.Instance == null)
                {
                    Debug.LogError("FPEInteractionManagerScript:: There is no FPEMenu present in your scene. This will mean openMenu() and closeMenu() won't function as expected but the game will still run.");
                }

                rememberStartingMouseSensitivity();
                refreshAlternateMouseSensitivities();

                interactionLabelLargestScale = new Vector3(1.0f, 1.0f, 1.0f);
                interactionLabelSmallestScale = new Vector3(0.0f, 0.0f, 0.0f);

                audioDiaryLabelLargestScale = new Vector3(1.1f, 1.1f, 1.1f);
                audioDiaryLabelSmallestScale = new Vector3(0.9f, 0.9f, 0.9f);

                defaultDiaryColor = audioDiaryLabel.GetComponent<Text>().color;
                diaryFadeColor = audioDiaryLabel.GetComponent<Text>().color;
                diaryFadeColor.a = 0.0f;

                notificationLabelLargestScale = new Vector3(1.1f, 1.1f, 1.1f);
                notificationLabelSmallestScale = new Vector3(0.9f, 0.9f, 0.9f);

                defaultNotificationColor = notificationLabel.GetComponent<Text>().color;
                notificationFadeColor = notificationLabel.GetComponent<Text>().color;
                notificationFadeColor.a = 0.0f;

                unZoomedFOV = Camera.main.fieldOfView;

                closeJournal(false);
                hideAudioDiaryTitle();
                setMouseHints("", "", "");

                setCursorVisibility(false);
                setCursorTexture(journalCursor);

            }

        }


        void Update()
        {

            if (menuOpen)
            {

                // Close Menu //
                if (FPEInputManager.Instance.GetButtonDown(FPEInputManager.eFPEInput.FPE_INPUT_MENU))
                {
                    closeMenu();
                }

            }
            else
            {

                #region CORE_INTERACTION_LOGIC

                #region CHECK_FOR_INTERACTABLE_OBJECT

                Ray interactionRay = Camera.main.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
                RaycastHit hitInteractable;
                Physics.Raycast(interactionRay, out hitInteractable, interactionRange, interactionLayerMask);

                // Check if we're looking at InteractableBase-type object //
                if (!examiningObject && !dockingInProgress && hitInteractable.transform != null && hitInteractable.transform.gameObject.GetComponent<FPEInteractableBaseScript>())
                {

                    if (hitInteractable.distance < hitInteractable.transform.gameObject.GetComponent<FPEInteractableBaseScript>().getInteractionDistance())
                    {

                        if (currentInteractableObject)
                        {
                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().unHighlightObject();
                            currentInteractableObject = null;
                        }

                        currentInteractableObject = hitInteractable.transform.gameObject;

                        // This is the one case where we retrieve notes from objects by looking at them
                        if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.STATIC)
                        {
                            retrieveNote(currentInteractableObject);
                            retrievePassiveAudioDiary(currentInteractableObject, false);
                        }
                        else
                        {
                            // Notes are not retrieved passively, only diaries
                            retrievePassiveAudioDiary(currentInteractableObject, false);
                        }

                        if (slowMouseOnInteractableObjectHighlight)
                        {
                            setMouseSensitivity(highlightedMouseSensitivity);
                        }

                        updateControlHints();

                    }
                    else
                    {

                        if (currentInteractableObject)
                        {

                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().unHighlightObject();
                            currentInteractableObject = null;
                            // If not in range, you could do some kind of additional "reticle hint" to override reticle state (icon only) to show "hey, there's something over there"?
                            updateControlHints();

                        }

                    }

                }
                // Check if we're looking at Put Back Location //
                else
                {

                    // Check for Put Back Location //
                    Ray rayPutBack = Camera.main.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
                    RaycastHit hitPutBack2;
                    Physics.Raycast(rayPutBack, out hitPutBack2, interactionRange, putbackLayerMask);

                    // Only allow Put Back if we're holding an object and not currently examining it
                    if (!examiningObject && hitPutBack2.transform != null && hitPutBack2.transform.gameObject.GetComponent<FPEPutBackScript>() && currentHeldObject != null)
                    {

                        if (hitPutBack2.transform.gameObject.GetComponent<FPEPutBackScript>().putBackMatchesGameObject(currentHeldObject) && (hitPutBack2.distance < hitPutBack2.transform.gameObject.GetComponent<FPEPutBackScript>().getInteractionDistance()))
                        {
                            currentPutbackObject = hitPutBack2.transform.gameObject;
                        }
                        else
                        {
                            currentPutbackObject = null;
                        }

                        if (slowMouseOnInteractableObjectHighlight)
                        {
                            setMouseSensitivity(highlightedMouseSensitivity);
                        }

                        updateControlHints();

                    }
                    // I guess we're looking at NOTHING //
                    else
                    {

                        if (currentInteractableObject)
                        {
                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().unHighlightObject();
                            currentInteractableObject = null;
                        }

                        currentPutbackObject = null;

                        updateControlHints();

                        if (!cameraZoomedIn)
                        {
                            restorePreviousMouseSensitivity(true);
                        }

                    }

                }
                #endregion

                #region HANDLE_INTERACTIONS
                // Pick up / Put down / Interact / Read / Activate / Etc. //
                if (FPEInputManager.Instance.GetButtonDown(FPEInputManager.eFPEInput.FPE_INPUT_INTERACT) && !examiningObject && !dockingInProgress)
                {

                    // Already holding an object //
                    if (currentHeldObject)
                    {

                        // If I was looking at a valid Put Back location, put the object back
                        if (currentPutbackObject)
                        {

                            currentHeldObject.GetComponent<FPEInteractablePickupScript>().doPickupPutdown(true);
                            currentHeldObject.transform.position = currentPutbackObject.transform.position;
                            currentHeldObject.transform.rotation = currentPutbackObject.transform.rotation;
                            currentHeldObject.transform.parent = null;
                            currentHeldObject.GetComponent<Collider>().isTrigger = false;
                            currentHeldObject.GetComponent<Rigidbody>().isKinematic = false;
                            currentHeldObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
                            currentHeldObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

                            Transform[] objectTransforms = currentHeldObject.GetComponentsInChildren<Transform>();

                            foreach (Transform t in objectTransforms)
                            {
                                t.gameObject.layer = LayerMask.NameToLayer("FPEPickupObjects");
                            }

                            currentHeldObject = null;
                            currentPutbackObject = null;
                            // Set scale of text to smallest size to create a pseudo animation between seeing "pick up" and "put back" strings
                            interactionLabel.localScale = interactionLabelSmallestScale;

                            updateControlHints();

                        }
                        // If I am already holding something, and allowed to interact with the currently highlighted thing while there's something in my hand, do that
                        else if (currentInteractableObject && currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionsAllowedWhenHoldingObject() == true)
                        {

                            if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.STATIC)
                            {
                                retrieveNote(currentInteractableObject);
                                retrievePassiveAudioDiary(currentInteractableObject, true);
                                currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interact();
                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.ACTIVATE)
                            {
                                retrieveNote(currentInteractableObject);
                                retrievePassiveAudioDiary(currentInteractableObject, true);
                                attemptActivation(currentInteractableObject);
                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.PICKUP)
                            {
                                // Player can only ever hold one thing, regardless of this value.
                                retrievePassiveAudioDiary(currentInteractableObject, true);
                            }
                            
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.JOURNAL)
                            {
                                // Journals always require two hands
                                retrievePassiveAudioDiary(currentInteractableObject, true);
                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.INVENTORY)
                            {
                                // See "Put In Inventory" section below
                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.DOCK)
                            {

                                if (currentInteractableObject.GetComponent<FPEInteractableDockScript>().isOccupied() == false)
                                {
                                    DockPlayer(currentInteractableObject.GetComponent<FPEInteractableDockScript>());
                                    retrieveNote(currentInteractableObject);
                                    retrievePassiveAudioDiary(currentInteractableObject, true);
                                }

                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.AUDIODIARY)
                            {
                                if (currentInteractableObject.GetComponent<FPEInteractableAudioDiaryScript>().AutomaticPlayback == false)
                                {
                                    retrieveNote(currentInteractableObject);
                                    // Note: We do NOT retrieve Audio Diary from AUDIO DIARY type, because they are already diaries :)
                                    currentInteractableObject.GetComponent<FPEInteractableAudioDiaryScript>().interact();
                                }
                            }
                            /*
                            // Note: You can add a case here to handle any new interaction types you create that should also allow interactions when object is being held. Be sure to add it to the eInteractionType enum in FPEInteractableBaseScript
                            else if(currentInteractionObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.YOUR_NEW_TYPE_HERE)
                            {
                                // YOUR CUSTOM INTERACTION LOGIC HERE
                            }
                            */
                            else
                            {
                                Debug.LogWarning("FPEInteractionManagerScript:: Unhandled eInteractionType '" + currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() + "' for object that allows interaction when object being held. No case exists to manage this.");
                            }

                            updateControlHints();

                        }
                        // If I am already holding something, and NOT allowed to interact with the currently highlighted thing while there's something in my hand
                        else if (currentInteractableObject && currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionsAllowedWhenHoldingObject() == false)
                        {

                            if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.STATIC)
                            {
                                currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interact();
                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.ACTIVATE)
                            {
                                // If activation is not permitted, we do nothing
                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.PICKUP)
                            {
                                // Player can only ever hold one thing, regardless of this value.
                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.JOURNAL)
                            {
                                // Journals always require two hands
                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.INVENTORY)
                            {
                                // See "Put In Inventory" section below
                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.DOCK)
                            {
                                // If this dock requires 2 hands, do nothing
                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.AUDIODIARY)
                            {
                                // Audio Diaries can always be played when something is in player's hand (see cases above)
                            }
                            /*
                            // Note: You can add a case here to handle any new interaction types you create that should also allow interactions when object is being held. Be sure to add it to the eInteractionType enum in FPEInteractableBaseScript
                            else if(currentInteractionObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.YOUR_NEW_TYPE_HERE)
                            {
                                // YOUR CUSTOM INTERACTION LOGIC HERE
                            }
                            */
                            else
                            {
                                Debug.LogWarning("FPEInteractionManagerScript:: Unhandled eInteractionType '" + currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() + "' for object that DOES NOT allow interaction when object being held. No case exists to manage this.");
                            }

                        }
                        // Otherwise, if we're not putting the object back, just toss it
                        else
                        {

                            currentHeldObject.GetComponent<FPEInteractablePickupScript>().doPickupPutdown(false);
                            tossObject(currentHeldObject.GetComponent<FPEInteractablePickupScript>());
                            currentHeldObject = null;
                            updateControlHints();

                        }

                    }
                    // Not already holding an object //
                    else
                    {

                        // If we're looking at an object, we need to handle the various interaction types (pickup, activate, etc.)
                        if (currentInteractableObject)
                        {

                            lastObjectHeldRotation = Quaternion.identity;

                            if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.PICKUP)
                            {

                                retrieveNote(currentInteractableObject);
                                retrievePassiveAudioDiary(currentInteractableObject, true);

                                currentInteractableObject.GetComponent<FPEInteractablePickupScript>().doPickupPutdown(false);
                                currentInteractableObject.GetComponent<FPEInteractableBaseScript>().unHighlightObject();
                                moveObjectToPlayersHand(currentInteractableObject);
                                currentInteractableObject = null;
                                interactionLabel.localScale = interactionLabelSmallestScale;

                                // Un-zoom, restore mouse state
                                cameraZoomedIn = false;
                                restorePreviousMouseSensitivity(false);

                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.STATIC)
                            {
                                currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interact();
                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.JOURNAL)
                            {

                                currentInteractableObject.GetComponent<FPEInteractableJournalScript>().activateJournal();
                                currentJournal = currentInteractableObject;
                                cameraZoomedIn = false;
                                restorePreviousMouseSensitivity(false);
                                openJournal();
                                retrieveNote(currentInteractableObject);
                                retrievePassiveAudioDiary(currentInteractableObject, true);

                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.ACTIVATE)
                            {

                                retrieveNote(currentInteractableObject);
                                retrievePassiveAudioDiary(currentInteractableObject, true);
                                attemptActivation(currentInteractableObject);

                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.INVENTORY)
                            {

                                // Treat the same as Pickup type, if permitted
                                if (currentInteractableObject.GetComponent<FPEInteractableInventoryItemScript>().PickupPermitted)
                                {

                                    retrieveNote(currentInteractableObject);
                                    retrievePassiveAudioDiary(currentInteractableObject, true);

                                    currentInteractableObject.GetComponent<FPEInteractableInventoryItemScript>().doPickupPutdown(false);
                                    currentInteractableObject.GetComponent<FPEInteractableBaseScript>().unHighlightObject();
                                    moveObjectToPlayersHand(currentInteractableObject);
                                    currentInteractableObject = null;
                                    interactionLabel.localScale = interactionLabelSmallestScale;

                                    // Un-zoom, restore mouse state
                                    cameraZoomedIn = false;
                                    restorePreviousMouseSensitivity(false);

                                }

                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.DOCK)
                            {

                                if(currentInteractableObject.GetComponent<FPEInteractableDockScript>().isOccupied() == false)
                                {

                                    DockPlayer(currentInteractableObject.GetComponent<FPEInteractableDockScript>());
                                    retrieveNote(currentInteractableObject);
                                    retrievePassiveAudioDiary(currentInteractableObject, true);

                                }

                            }
                            else if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.AUDIODIARY)
                            {

                                if (currentInteractableObject.GetComponent<FPEInteractableAudioDiaryScript>().AutomaticPlayback == false)
                                {

                                    retrieveNote(currentInteractableObject);
                                    // Note: We do NOT retrieve Audio Diary from AUDIO DIARY type, because they are already diaries :)
                                    currentInteractableObject.GetComponent<FPEInteractableAudioDiaryScript>().interact();

                                }

                            }
                            /*
                            // Note: You can add a case here to handle any new interaction types you create. Be sure to add it to the eInteractionType enum in FPEInteractableBaseScript
                            else if(currentInteractionObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.YOUR_NEW_TYPE_HERE)
                            {
                                // YOUR CUSTOM INTERACTION LOGIC HERE
                            }
                            */
                            else
                            {
                                Debug.LogWarning("FPEInteractionManagerScript:: Unhandled eInteractionType '" + currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() + "'. No case exists to manage this (Which might be just fine, depending).");
                            }

                            updateControlHints();

                        }

                    }

                }
                #endregion

                #region HANDLE_EXAMINATION_START_STOP
                // Examine held object //
                if (FPEInputManager.Instance.GetButtonDown(FPEInputManager.eFPEInput.FPE_INPUT_EXAMINE) && currentHeldObject && !dockingInProgress)
                {

                    hideReticleAndInteractionLabel();
                    examiningObject = true;
                    currentHeldObject.GetComponent<FPEInteractablePickupScript>().startExamination();
                    disableMouseLook();
                    disableMovement();

                    if (currentHeldObject.GetComponent<FPEInteractablePickupScript>().postExaminationInteractionString != "")
                    {
                        currentHeldObject.GetComponent<FPEInteractablePickupScript>().interactionString = currentHeldObject.GetComponent<FPEInteractablePickupScript>().postExaminationInteractionString;
                    }

                    currentHeldObject.transform.position = interactionObjectExamineLocation.transform.position;

                    if (currentHeldObject.GetComponent<FPEInteractablePickupScript>().rotationLockType == FPEInteractablePickupScript.eRotationType.FREE)
                    {

                        if (lastObjectHeldRotation == Quaternion.identity)
                        {

                            Vector3 relativePos = Camera.main.transform.position - currentHeldObject.transform.position;
                            Quaternion rotation = Quaternion.LookRotation(relativePos);
                            currentHeldObject.transform.rotation = rotation;
                            // The first time we pick something up, we apply this additional rotation offset to ensure it is oriented correctly. Subsequent pickups just yield to last rotation made when examining object
                            currentHeldObject.transform.Rotate(currentHeldObject.GetComponent<FPEInteractablePickupScript>().pickupRotationOffset);

                        }
                        else
                        {
                            currentHeldObject.transform.rotation = lastObjectHeldRotation;
                        }

                    }
                    else
                    {

                        Vector3 relativePos = Camera.main.transform.position - currentHeldObject.transform.position;
                        Quaternion rotation = Quaternion.LookRotation(relativePos);
                        currentHeldObject.transform.rotation = rotation;

                    }

                }

                // Stop examining held object //
                if (FPEInputManager.Instance.GetButtonUp(FPEInputManager.eFPEInput.FPE_INPUT_EXAMINE) && currentHeldObject)
                {

                    lastObjectHeldRotation = currentHeldObject.transform.rotation;
                    examiningObject = false;
                    showReticleAndInteractionLabel();
                    currentHeldObject.GetComponent<FPEInteractablePickupScript>().endExamination();
                    enableMouseLook();
                    enableMovement();

                }

                #endregion

                #region HANDLE_BEHAVIOUR_WHILE_HOLDING_OBJECT
                // Behaviours when holding an object //
                if (currentHeldObject)
                {

                    #region EXAMINING
                    if (examiningObject)
                    {

                        // Examination logic: Position, Rotation, etc.
                        float examinationOffsetUp = currentHeldObject.GetComponent<FPEInteractablePickupScript>().examinationOffsetUp;
                        float examinationOffsetForward = currentHeldObject.GetComponent<FPEInteractablePickupScript>().examinationOffsetForward;
                        currentHeldObject.transform.position = interactionObjectExamineLocation.transform.position + Vector3.up * examinationOffsetUp + interactionObjectExamineLocation.transform.forward * examinationOffsetForward;
                        float rotationInputX = 0.0f;
                        float rotationInputY = 0.0f;
                        float examinationChangeX = FPEInputManager.Instance.GetAxis(FPEInputManager.eFPEInput.FPE_INPUT_MOUSELOOKX);
                        float examinationChangeY = FPEInputManager.Instance.GetAxis(FPEInputManager.eFPEInput.FPE_INPUT_MOUSELOOKY);

                        // If there was no mouse input use gamepad instead
                        if (examinationChangeX == 0 & examinationChangeY == 0)
                        {

                            // Note: We scale our gamepad by delta time because it's not a "change since last frame" like mouse 
                            // input, so we need to simulate that ourselves.
                            examinationChangeX = FPEInputManager.Instance.GetAxis(FPEInputManager.eFPEInput.FPE_INPUT_LOOKX) * Time.deltaTime;
                            examinationChangeY = FPEInputManager.Instance.GetAxis(FPEInputManager.eFPEInput.FPE_INPUT_LOOKY) * Time.deltaTime;

                        }

                        if (Mathf.Abs(examinationChangeX) > examinationDeadzone)
                        {
                            rotationInputX = -(examinationChangeX * examineRotationSpeed);
                        }

                        if (Mathf.Abs(examinationChangeY) > examinationDeadzone)
                        {
                            rotationInputY = (examinationChangeY * examineRotationSpeed);
                        }

                        switch (currentHeldObject.GetComponent<FPEInteractablePickupScript>().rotationLockType)
                        {

                            case FPEInteractablePickupScript.eRotationType.FREE:
                                currentHeldObject.transform.Rotate(interactionObjectExamineLocation.transform.up, rotationInputX, Space.World);
                                currentHeldObject.transform.Rotate(interactionObjectExamineLocation.transform.right, rotationInputY, Space.World);
                                break;
                            case FPEInteractablePickupScript.eRotationType.HORIZONTAL:
                                currentHeldObject.transform.Rotate(interactionObjectExamineLocation.transform.up, rotationInputX, Space.World);
                                break;
                            case FPEInteractablePickupScript.eRotationType.VERTICAL:
                                currentHeldObject.transform.Rotate(interactionObjectExamineLocation.transform.right, rotationInputY, Space.World);
                                break;
                            case FPEInteractablePickupScript.eRotationType.NONE:
                            default:
                                break;

                        }

                    }
                    #endregion

                    #region NOT_EXAMINING
                    else
                    {

                        // Update position of object to be that of holding position
                        currentHeldObject.transform.position = interactionObjectPickupLocation.transform.position;
                        // Lerp a bit so it feels less rigid and more like holding something in real life
                        currentHeldObject.transform.rotation = Quaternion.Slerp(currentHeldObject.transform.rotation, interactionObjectPickupLocation.transform.rotation * Quaternion.Euler(lastObjectHeldRotation.eulerAngles + currentHeldObject.GetComponent<FPEInteractablePickupScript>().pickupRotationOffset), 0.2f);

                        updateControlHints();

                    }
                    #endregion

                }
                #endregion

                #region HANDLE_PUT_IN_INVENTORY
                // Put object in inventory //
                if (FPEInputManager.Instance.GetButtonDown(FPEInputManager.eFPEInput.FPE_INPUT_PUT_IN_INVENTORY) && !dockingInProgress)
                {

                    // Already holding an object //
                    if (currentHeldObject)
                    {

                        if (!examiningObject)
                        {

                            // Case 1: Player is holding an item in their hand, but looking at another inventory item that they want to take. Note that this case must just yield
                            // to the "take, no pickup allowed" behaviour, as such the mouse hints must change to reflect that.
                            if (currentInteractableObject)
                            {

                                if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.INVENTORY)
                                {
                                    putObjectIntoInventory(currentInteractableObject.GetComponent<FPEInteractableInventoryItemScript>());
                                }

                            }
                            // Case 2: Player is holding something and looking at nothing, so they can put the currently held object into (or back into) inventory
                            else
                            {

                                if (currentHeldObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.INVENTORY)
                                {

                                    putObjectIntoInventory(currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>());
                                    // In this specific case, the thing we were holding was the inventory item, so we need to nullify held object because we're no longer holding it
                                    currentHeldObject = null;

                                }

                            }

                        }

                    }
                    // Not already holding an object //
                    else
                    {

                        // Case 3: Player is looking at an inventory object in the world
                        if (currentInteractableObject)
                        {

                            if (currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() == FPEInteractableBaseScript.eInteractionType.INVENTORY)
                            {
                                putObjectIntoInventory(currentInteractableObject.GetComponent<FPEInteractableInventoryItemScript>());
                            }

                        }

                    }

                    updateControlHints();

                }
                #endregion

                #region HANDLE_CLOSE_AND_MENU_BUTTONS
                // Skip Audio Diary, Close Journal, Undock player (or other future non-menu UI items) //
                if (FPEInputManager.Instance.GetButtonDown(FPEInputManager.eFPEInput.FPE_INPUT_CLOSE))
                {

                    if (currentJournal)
                    {
                        closeJournal();
                    }
                    else if (currentDock && !examiningObject)
                    {
                        UnDockPlayer(currentDock.GetComponent<FPEInteractableDockScript>().SmoothDock);
                    }
                    else
                    {

                        // Note it's possible to be playing "Audio Log" style sounds through an Interactable object that is NOT of type AudioDiary (see demoJournal, for example)
                        if (playingAudioDiary)
                        {

                            if (currentAudioDiary)
                            {
                                currentAudioDiary.GetComponent<FPEInteractableAudioDiaryScript>().stopAudioDiary();
                            }

                            fadingDiaryAudio = true;
                            fadingDiaryText = true;
                            audioDiarySkipHintLabel.gameObject.SetActive(false);

                        }

                    }

                }

                // Open Menu or Close Journal) //
                if (FPEInputManager.Instance.GetButtonDown(FPEInputManager.eFPEInput.FPE_INPUT_MENU) && !dockingInProgress)
                {

                    if (currentJournal)
                    {
                        closeJournal();
                    }
                    else
                    {

                        if (!examiningObject)
                        {
                            openMenu();
                        }

                    }

                }
                #endregion

                #endregion

                if (dockingInProgress)
                {

                    // Here we wait on the player controller to tell us when the player is done docking.
                    if (dockingCompleted())
                    {

                        dockingInProgress = false;

                        if(currentDockActionType == FPEFirstPersonController.ePlayerDockingState.DOCKING)
                        {
                            currentDock.GetComponent<FPEInteractableDockScript>().finishDocking();
                        }
                        else if(currentDockActionType == FPEFirstPersonController.ePlayerDockingState.UNDOCKING)
                        {
                            currentDock.GetComponent<FPEInteractableDockScript>().finishUnDocking();
                            currentDock = null;
                        }
                        else
                        {
                            Debug.LogWarning("FPEInteractionManagerScript:: Dock action finished, but was not dock or undock, which makes no sense. Did you start a dock using alternate functions?");
                        }

                        unhideAllUI();

                    }

                    updateControlHints();

                }

                #region CAMERA_ZOOM
                // Camera Zoom - don't allow when holding an object or reading a journal //
                if (currentHeldObject == null && currentJournal == null)
                {

                    if (FPEInputManager.Instance.GetButtonDown(FPEInputManager.eFPEInput.FPE_INPUT_ZOOM))
                    {
                        setMouseSensitivity(zoomedMouseSensitivity);
                    }
                    if (FPEInputManager.Instance.GetButtonUp(FPEInputManager.eFPEInput.FPE_INPUT_ZOOM))
                    {
                        restorePreviousMouseSensitivity(false);
                    }
                    if (FPEInputManager.Instance.GetButton(FPEInputManager.eFPEInput.FPE_INPUT_ZOOM))
                    {
                        cameraZoomedIn = true;
                    }
                    else
                    {
                        cameraZoomedIn = false;
                    }

                }

                // Change actual camera FOV based on zoom state //
                if (cameraZoomedIn)
                {
                    Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, zoomedFOV, cameraZoomChangeRate);
                }
                else
                {
                    Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, unZoomedFOV, cameraZoomChangeRate);
                }

                #endregion

                // Animate the size of the interaction text //
                interactionLabel.localScale = Vector3.Lerp(interactionLabel.localScale, interactionLabelTargetScale, 0.25f);

                #region DIARY_MANAGEMENT

                // Animate audio diary title when visible //
                if (playingAudioDiary)
                {

                    audioDiaryLabel.localScale = Vector3.Lerp(audioDiaryLabel.localScale, audioDiaryLabelTargetScale, notificationScaleLerpFactor * Time.deltaTime);

                    if ((audioDiaryLabelTargetScale == audioDiaryLabelLargestScale) && (Vector3.Distance(audioDiaryLabel.localScale, audioDiaryLabelLargestScale) < 0.1f))
                    {
                        audioDiaryLabelTargetScale = audioDiaryLabelSmallestScale;
                    }
                    else if ((audioDiaryLabelTargetScale == audioDiaryLabelSmallestScale) && (Vector3.Distance(audioDiaryLabel.localScale, audioDiaryLabelSmallestScale) < 0.1f))
                    {
                        audioDiaryLabelTargetScale = audioDiaryLabelLargestScale;
                    }

                }

                // Fade out diary text when done playing //
                if (fadingDiaryText)
                {

                    audioDiaryLabel.GetComponent<Text>().color = Color.Lerp(audioDiaryLabel.GetComponent<Text>().color, diaryFadeColor, notificationColorLerpFactor * Time.deltaTime);

                    if (audioDiaryLabel.GetComponent<Text>().color.a <= 0.1f)
                    {
                        audioDiaryLabel.GetComponent<Text>().text = "";
                        audioDiaryLabel.GetComponent<Text>().color = defaultDiaryColor;
                        fadingDiaryText = false;
                    }

                }

                // Fading out diary audio track //
                if (playingAudioDiary && !audioDiaryPlayer.GetComponent<AudioSource>().isPlaying)
                {

                    if (currentAudioDiary)
                    {
                        currentAudioDiary.GetComponent<FPEInteractableAudioDiaryScript>().stopAudioDiary();
                    }

                    hideAudioDiaryTitle();

                }

                #endregion

                #region NOTIFICATION_MANAGEMNT

                // Make notification pulse a bit
                if (notificationCounter > 0.0f)
                {

                    notificationLabel.localScale = Vector3.Lerp(notificationLabel.localScale, notificationLabelTargetScale, notificationScaleLerpFactor * Time.deltaTime);

                    if ((notificationLabelTargetScale == notificationLabelLargestScale) && (Vector3.Distance(notificationLabel.localScale, notificationLabelLargestScale) < 0.1f))
                    {
                        notificationLabelTargetScale = notificationLabelSmallestScale;
                    }
                    else if ((notificationLabelTargetScale == notificationLabelSmallestScale) && (Vector3.Distance(notificationLabel.localScale, notificationLabelSmallestScale) < 0.1f))
                    {
                        notificationLabelTargetScale = notificationLabelLargestScale;
                    }

                    notificationCounter -= Time.deltaTime;

                    if(notificationCounter <= 0.0f)
                    {
                        fadingNotificationText = true;
                    }

                }

                // Fade out our notification
                if (fadingNotificationText)
                {

                    notificationLabel.GetComponent<Text>().color = Color.Lerp(notificationLabel.GetComponent<Text>().color, notificationFadeColor, notificationColorLerpFactor*Time.deltaTime);

                    if (notificationLabel.GetComponent<Text>().color.a <= 0.1f)
                    {
                        notificationLabel.GetComponent<Text>().text = "";
                        notificationLabel.GetComponent<Text>().color = defaultNotificationColor;
                        fadingNotificationText = false;
                    }

                }

                #endregion

                // Mouse sensitivity transition smoothing. When slowMouseOnInteractableObjectHighlight is true, we want the mouse sensitivity to somewhat smoothly restore
                // to "full" sensitivity, but not be jarring or "pop" as soon as an object is no longer highlighted by the reticle.
                // Note: Replace this as required if using a different FPS Character Controller like UFPS, etc. that has other integrated mouse sensitivity management and aim assist.
                if (smoothMouseChange)
                {
                    FPEInputManager.Instance.LookSensitivity = Vector2.Lerp(FPEInputManager.Instance.LookSensitivity, new Vector2(targetMouseSensitivity.x, targetMouseSensitivity.y), smoothMouseChangeRate*Time.deltaTime);
                }

            }

            // We want to fade out audio for the diary regardless of menu state. This covers the case where the player skips a diary, then opens the menu. 
            if (fadingDiaryAudio)
            {

                // We use unscaled delta time in case the player skips then opens the menu. We want the audio to continue fading out.
                fadeCounter += Time.unscaledDeltaTime;

                if (fadeCounter >= 0.1f)
                {

                    audioDiaryPlayer.GetComponent<AudioSource>().volume -= fadeAmountPerTenthSecond;

                    if (audioDiaryPlayer.GetComponent<AudioSource>().volume <= 0.0f)
                    {
                        audioDiaryPlayer.GetComponent<AudioSource>().Stop();
                        fadingDiaryAudio = false;
                    }

                    fadeCounter = 0.0f;

                }

            }

        }


        private void activateReticle(string interactionString, string additionalHint = "")
        {

            if (reticleEnabled)
            {
                reticle.GetComponent<Image>().overrideSprite = ((additionalHint == "") ? activeReticle : unavailableReticle);
            }

            if (interactionTextEnabled)
            {
                interactionLabel.GetComponent<Text>().text = interactionString + ((additionalHint != "") ? "\n" + additionalHint : "");
                interactionLabelTargetScale = interactionLabelLargestScale;
            }

        }

        private void deactivateReticle()
        {

            if (reticleEnabled)
            {
                reticle.GetComponent<Image>().overrideSprite = inactiveReticle;
            }

            if (interactionTextEnabled)
            {
                interactionLabel.GetComponent<Text>().text = "";
                interactionLabelTargetScale = interactionLabelSmallestScale;
            }

        }

        private void hideReticleAndInteractionLabel()
        {

            if (reticleEnabled)
            {
                reticle.GetComponentInChildren<Image>().enabled = false;
            }

            if (interactionTextEnabled)
            {
                interactionLabel.GetComponentInChildren<Text>().text = "";
            }

        }

        private void showReticleAndInteractionLabel()
        {

            if (reticleEnabled)
            {
                reticle.GetComponentInChildren<Image>().enabled = true;
            }

            if (interactionTextEnabled)
            {
                interactionLabel.GetComponentInChildren<Text>().text = "";
            }

        }

        public void playNewAudioDiary(GameObject diary)
        {

            // if currently playing an audio diary, stop current one, reset text for new one
            if (playingAudioDiary)
            {

                if (currentAudioDiary)
                {
                    currentAudioDiary.GetComponent<FPEInteractableAudioDiaryScript>().stopAudioDiary();
                }

                audioDiaryLabel.GetComponent<Text>().text = "";
                audioDiaryLabel.GetComponent<Text>().color = defaultDiaryColor;

            }

            currentAudioDiary = diary.gameObject;
            audioDiaryLabel.GetComponent<Text>().color = defaultDiaryColor;
            audioDiaryLabel.GetComponent<Text>().text = audioDiaryPlayingPreText + " '" + diary.GetComponent<FPEInteractableAudioDiaryScript>().audioDiaryTitle + "' " + audioDiaryPlayingPostText;
            playingAudioDiary = true;
            audioDiaryLabelTargetScale = audioDiaryLabelLargestScale;
            audioDiarySkipHintLabel.gameObject.SetActive(true);
            fadingDiaryAudio = false;
            fadingDiaryText = false;
            audioDiaryPlayer.GetComponent<AudioSource>().clip = diary.GetComponent<FPEInteractableAudioDiaryScript>().audioDiaryClip;
            audioDiaryPlayer.GetComponent<AudioSource>().volume = 1.0f;
            audioDiaryPlayer.GetComponent<AudioSource>().Play();

            // Also check to see if we should add an entry in the audio diaries menu for this specific audio diary
            if (diary.GetComponent<FPEInteractableAudioDiaryScript>().AddEntryToInventory)
            {
                inventoryManager.addAudioDiaryEntry(new FPEAudioDiaryEntry(diary.GetComponent<FPEInteractableAudioDiaryScript>().audioDiaryTitle, diary.GetComponent<FPEInteractableAudioDiaryScript>().audioDiaryClip, diary.GetComponent<FPEInteractableAudioDiaryScript>().ShowDiaryTitle));
            }

        }

        public void playAudioDiaryEntry(FPEAudioDiaryEntry diaryEntry)
        {

            // Case where we are interupting an existing diary from in the world
            if (playingAudioDiary)
            {

                if (currentAudioDiary)
                {
                    currentAudioDiary.GetComponent<FPEInteractableAudioDiaryScript>().stopAudioDiary();
                }

                audioDiaryLabel.GetComponent<Text>().text = "";
                audioDiaryLabel.GetComponent<Text>().color = defaultDiaryColor;

            }

            currentAudioDiary = null;

            audioDiaryLabel.GetComponent<Text>().color = defaultDiaryColor;
            if (diaryEntry.ShowDiaryTitle)
            {
                audioDiaryLabel.GetComponent<Text>().text = audioDiaryReplayPreText + " '" + diaryEntry.DiaryTitle + "' " + audioDiaryPlayingPostText;
            }
            else
            {
                audioDiaryLabel.GetComponent<Text>().text = "";
            }

            playingAudioDiary = true;
            audioDiaryLabelTargetScale = audioDiaryLabelLargestScale;
            audioDiarySkipHintLabel.gameObject.SetActive(true);
            fadingDiaryAudio = false;
            fadingDiaryText = false;
            audioDiaryPlayer.GetComponent<AudioSource>().clip = diaryEntry.DiaryAudio;
            audioDiaryPlayer.GetComponent<AudioSource>().volume = 1.0f;
            audioDiaryPlayer.GetComponent<AudioSource>().Play();

        }

        /// <summary>
        /// Provides a means to do a hard stop of all audio diary playback. No text or audio fade outs will occur.
        /// </summary>
        public void stopAllDiaryPlayback()
        {

            if (playingAudioDiary)
            {

                if (currentAudioDiary)
                {
                    currentAudioDiary.GetComponent<FPEInteractableAudioDiaryScript>().stopAudioDiary();
                }

                audioDiaryLabel.GetComponent<Text>().text = "";
                audioDiaryLabel.GetComponent<Text>().color = defaultDiaryColor;
                audioDiarySkipHintLabel.gameObject.SetActive(false);
                audioDiaryPlayer.GetComponent<AudioSource>().volume = 0.0f;
                audioDiaryPlayer.GetComponent<AudioSource>().Stop();

            }

        }

        public void playSecondaryInteractionAudio(AudioClip secondaryClip, bool playAsAudioDiary, string audioLogText = "")
        {

            // if currently playing an audio diary, stop current one, reset text for new one
            if (playingAudioDiary)
            {

                if (currentAudioDiary)
                {
                    currentAudioDiary.GetComponent<FPEInteractableAudioDiaryScript>().stopAudioDiary();
                }

                audioDiaryLabel.GetComponent<Text>().text = "";
                audioDiaryLabel.GetComponent<Text>().color = defaultDiaryColor;

            }

            if (playAsAudioDiary && audioLogText != "")
            {

                audioDiaryLabel.GetComponent<Text>().color = defaultDiaryColor;
                audioDiaryLabel.GetComponent<Text>().text = "Playing '" + audioLogText + "'";
                playingAudioDiary = true;
                audioDiaryLabelTargetScale = audioDiaryLabelLargestScale;
                audioDiarySkipHintLabel.gameObject.SetActive(true);
                fadingDiaryAudio = false;
                fadingDiaryText = false;
                audioDiaryPlayer.GetComponent<AudioSource>().clip = secondaryClip;
                audioDiaryPlayer.GetComponent<AudioSource>().volume = 1.0f;
                audioDiaryPlayer.GetComponent<AudioSource>().Play();

            }
            else
            {

                secondaryInteractionSoundPlayer.GetComponent<AudioSource>().clip = secondaryClip;
                secondaryInteractionSoundPlayer.GetComponent<AudioSource>().volume = 1.0f;
                secondaryInteractionSoundPlayer.GetComponent<AudioSource>().Play();

            }

        }

        public void stopSecondaryInteractionAudio()
        {
            secondaryInteractionSoundPlayer.GetComponent<AudioSource>().Stop();
        }

        public void hideAudioDiaryTitle()
        {

            fadingDiaryText = true;
            audioDiarySkipHintLabel.gameObject.SetActive(false);
            currentAudioDiary = null;
            playingAudioDiary = false;

        }

        #region JOURNAL
        public void openJournal()
        {

            disableMovement();
            disableMouseLook();
            setCursorVisibility(true);

            journalSFXPlayer.GetComponent<AudioSource>().clip = journalOpen;
            journalSFXPlayer.GetComponent<AudioSource>().Play();

            journalCloseIndicator.SetActive(true);
            journalPreviousButton.SetActive(true);
            journalNextButton.SetActive(true);
            journalBackground.SetActive(true);
            journalPage.SetActive(true);

            currentJournalPages = currentJournal.GetComponent<FPEInteractableJournalScript>().journalPages;

            if (currentJournalPages.Length > 0)
            {
                journalPage.transform.gameObject.GetComponentInChildren<Image>().overrideSprite = currentJournalPages[currentJournalPageIndex];
            }
            else
            {
                Debug.LogError("Journal '" + currentJournal.name + "' opened, but was assigned no pages. Assign Sprites to journalPages array in the Inspector.");
            }

        }

        public void nextJournalPage()
        {

            if (currentJournal)
            {

                currentJournalPageIndex++;

                if (currentJournalPageIndex > currentJournalPages.Length - 1)
                {
                    currentJournalPageIndex = currentJournalPages.Length - 1;
                }
                else
                {
                    journalSFXPlayer.GetComponent<AudioSource>().clip = journalPageTurn;
                    journalSFXPlayer.GetComponent<AudioSource>().Play();
                }

                journalPage.transform.gameObject.GetComponentInChildren<Image>().overrideSprite = currentJournalPages[currentJournalPageIndex];

            }

        }

        public void previousJournalPage()
        {

            if (currentJournal)
            {

                currentJournalPageIndex--;

                if (currentJournalPageIndex < 0)
                {
                    currentJournalPageIndex = 0;
                }
                else
                {
                    journalSFXPlayer.GetComponent<AudioSource>().clip = journalPageTurn;
                    journalSFXPlayer.GetComponent<AudioSource>().Play();
                }

                journalPage.transform.gameObject.GetComponentInChildren<Image>().overrideSprite = currentJournalPages[currentJournalPageIndex];

            }

        }

        public void closeJournal(bool playSound = true)
        {

            journalCloseIndicator.SetActive(false);
            journalPreviousButton.SetActive(false);
            journalNextButton.SetActive(false);
            journalBackground.SetActive(false);
            journalPage.SetActive(false);

            if (currentJournal)
            {

                if (playSound)
                {
                    journalSFXPlayer.GetComponent<AudioSource>().clip = journalClose;
                    journalSFXPlayer.GetComponent<AudioSource>().Play();
                }

                currentJournal.GetComponent<FPEInteractableJournalScript>().deactivateJournal();

            }

            currentJournal = null;
            currentJournalPageIndex = 0;
            currentJournalPages = null;

            setCursorVisibility(false);
            enableMouseLook();
            enableMovement();

        }
        #endregion

        #region MENU
        public void openMenu()
        {

            if (!menuOpen && FPEMenu.Instance != null)
            {

                FPEMenu.Instance.activateMenu();
                menuOpen = true;
                Time.timeScale = 0.0f;
                disableMovement();
                disableMouseLook();
                setCursorVisibility(true);
                setMouseHints("", "", "");
                hideReticleAndInteractionLabel();

            }

        }

        public void closeMenu()
        {

            if (menuOpen && FPEMenu.Instance != null)
            {

                FPEMenu.Instance.deactivateMenu();
                setCursorVisibility(false);
                enableMouseLook();
                enableMovement();
                showReticleAndInteractionLabel();
                Time.timeScale = 1.0f;
                menuOpen = false;
                
                // Try to get cursor lock back when we close the UI
                OnApplicationFocus(true);

            }

        }
        #endregion

        private void moveObjectToPlayersHand(GameObject go)
        {

            go.GetComponent<Rigidbody>().isKinematic = true;

            Collider[] objectColliders = go.GetComponentsInChildren<Collider>();

            foreach (Collider c in objectColliders)
            {
                c.isTrigger = true;
            }

            go.transform.position = interactionObjectPickupLocation.transform.position;

            // We put the pickup into the player's hands so that it is very safely and easily not destroyed when we change levels
            go.transform.parent = interactionObjectPickupLocation.transform;

            currentHeldObject = go;

            // Move to examination layer so object being held/examined doesn't clip through other objects.
            Transform[] objectTransforms = go.GetComponentsInChildren<Transform>();

            foreach (Transform t in objectTransforms)
            {
                t.gameObject.layer = LayerMask.NameToLayer("FPEObjectExamination");
            }

        }

        /// <summary>
        /// Puts specified FPEInteractableInventoryItemScript into inventory. Both updates the internal 
        /// inventory data and physically moves and modifies the object as required.
        /// </summary>
        /// <param name="item">The FPEInteractableInventoryItemScript to add to inventory.</param>
        public void putObjectIntoInventory(FPEInteractableInventoryItemScript item, bool playSound = true)
        {

            // Check for notes about the object
            retrieveNote(item.gameObject);

            // Put object into the actual inventory //
            bool keepOriginal = inventoryManager.addInventoryItem(item);

            if (playSound)
            {
                genericSFXPlayer.GetComponent<AudioSource>().PlayOneShot(inventoryPickup);
            }

            if (keepOriginal)
            {

                // Then move it physically "out of the world" (just move and hide it really) //
                GameObject go = item.gameObject;
                go.GetComponent<Rigidbody>().isKinematic = true;

                Collider[] objectColliders = go.GetComponentsInChildren<Collider>();

                foreach (Collider c in objectColliders)
                {
                    c.isTrigger = true;
                }

                go.transform.position = interactionInventoryLocation.transform.position;
                go.transform.parent = interactionInventoryLocation.transform;

                // Move to examination layer so object being held/examined doesn't clip through other objects.
                Transform[] objectTransforms = go.GetComponentsInChildren<Transform>();

                foreach (Transform t in objectTransforms)
                {
                    t.gameObject.layer = LayerMask.NameToLayer("FPEObjectExamination");
                }

                // And disable it
                go.SetActive(false);

            }
            else
            {
                Destroy(item.gameObject);
            }

        }

        public void dropObjectFromInventory(FPEInteractableInventoryItemScript itemToDrop)
        {

            if(itemToDrop != null)
            {

                itemToDrop.gameObject.SetActive(true);
                tossObject(itemToDrop);

            }
            else
            {
                Debug.LogError("FPEInteractionManagerScript.dropObjectFromInventory() was given a null GameObject!");
            }

        }

        /// <summary>
        /// Call this function when loading a saved game and you need to place a previously saved object in the player's hand
        /// </summary>
        /// <param name="itemToHold"></param>
        public void holdObjectFromGameLoad(FPEInteractablePickupScript itemToHold)
        {
            moveObjectToPlayersHand(itemToHold.gameObject);
        }

        /// <summary>
        /// This function puts an inventory item into the player's hand
        /// </summary>
        /// <param name="itemToHold"></param>
        public void holdObjectFromInventory(FPEInteractableInventoryItemScript itemToHold)
        {

            FPEInteractableBaseScript.eInteractionType currentHeldType = FPEInteractionManagerScript.Instance.getHeldObjectType();

            // Case 1: Holding nothing
            // Put selected item into player hand
            if (currentHeldType == FPEInteractableBaseScript.eInteractionType.NULL_TYPE)
            {

                itemToHold.gameObject.SetActive(true);
                moveObjectToPlayersHand(itemToHold.gameObject);

            }

            // Case 2: Holding InventoryItem
            // Put held item back into inventory, and put selected item into player hand
            else if (currentHeldType == FPEInteractableBaseScript.eInteractionType.INVENTORY)
            {

                putObjectIntoInventory(currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>());
                itemToHold.gameObject.SetActive(true);
                moveObjectToPlayersHand(itemToHold.gameObject);

            }

            // Case 3: Holding Pickup object
            // Drop held Pickup item, and put selected item into player hand
            else if (currentHeldType == FPEInteractableBaseScript.eInteractionType.PICKUP)
            {

                tossObject(currentHeldObject.GetComponent<FPEInteractablePickupScript>());
                itemToHold.gameObject.SetActive(true);
                moveObjectToPlayersHand(itemToHold.gameObject);

            }

            // Case 4: Not supported
            else
            {
                Debug.Log("FPEInteractionManagerScript.holdObjectFromInventory():: Asked to hold unsupported type '" + currentHeldType + "' object. Nothing will be held.");
            }

        }

        /// <summary>
        /// Consumes an inventory item and fires its consumer script
        /// </summary>
        /// <param name="itemToConsume">The inventory item to consume</param>
        public void consumeObjectFromInventory(FPEInteractableInventoryItemScript itemToConsume)
        {

            FPEInventoryConsumer consumer = itemToConsume.gameObject.GetComponent<FPEInventoryConsumer>();
            if (consumer)
            {
                itemToConsume.gameObject.SetActive(true);
                consumer.consumeItem();
            }
            else
            {
                Debug.LogError("FPEInteractionManagerScript.consumeObjectFromInventory() asked to consume item '"+ itemToConsume.gameObject.name + "' but this item has no FPEInventoryConsumer attached. Destroying item instead!", itemToConsume.gameObject);
            }

        }


        /// <summary>
        /// Tosses any Pickup type Interactable object from the designated toss location. Note that 
        /// calling this a lot in short succession will likely result in physics weirdness if the 
        /// objects all overlap.
        /// </summary>
        /// <param name="pickup">The Pickup object you want to toss</param>
        public void tossObject(FPEInteractablePickupScript pickup)
        {

            GameObject go = pickup.gameObject;
            go.transform.parent = null;

            Collider[] objectColliders = go.GetComponentsInChildren<Collider>();

            foreach (Collider c in objectColliders)
            {
                c.isTrigger = false;
            }

            go.GetComponent<Rigidbody>().isKinematic = false;

            // Note: We move objects to a special toss location to prevent clipping into the player if the player tosses the object while walking forward
            float tossStrength = go.GetComponent<FPEInteractablePickupScript>().tossStrength;
            float tossOffsetUp = go.GetComponent<FPEInteractablePickupScript>().tossOffsetUp;
            float tossOffsetForward = go.GetComponent<FPEInteractablePickupScript>().tossOffsetForward;
            go.transform.position = interactionObjectTossLocation.transform.position + Vector3.up * tossOffsetUp + Camera.main.transform.forward * tossOffsetForward;
            go.GetComponent<Rigidbody>().AddForce(Camera.main.transform.forward * tossImpulseFactor * tossStrength, ForceMode.Impulse);

            Transform[] objectTransforms = go.GetComponentsInChildren<Transform>();

            foreach (Transform t in objectTransforms)
            {
                t.gameObject.layer = LayerMask.NameToLayer("FPEPickupObjects");
            }

            go.GetComponent<FPEInteractablePickupScript>().drop();

        }


        /// <summary>
        /// Checks current held object and returns its interaction type
        /// </summary>
        /// <returns>interaction type of current held object, or NULL_TYPE if nothing is being held</returns>
        public FPEInteractableBaseScript.eInteractionType getHeldObjectType()
        {
            return ((currentHeldObject == null) ? FPEInteractableBaseScript.eInteractionType.NULL_TYPE : currentHeldObject.GetComponent<FPEInteractableBaseScript>().getInteractionType());
        }


        /// <summary>
        /// Get current held object. Should only really be called by save game logic handler, and object should 
        /// not be modified outside this class and expected to work correctly.
        /// </summary>
        /// <returns>currently held Game Object, or null of no object is being held</returns>
        public GameObject getHeldObject()
        {
            return currentHeldObject;
        }


        private void retrieveNote(GameObject interactableObject)
        {

            if (interactableObject.gameObject.GetComponent<FPEAttachedNote>() && !interactableObject.gameObject.GetComponent<FPEAttachedNote>().Collected)
            {
                showNotification("New note '" + interactableObject.gameObject.GetComponent<FPEAttachedNote>().NoteTitle + "' added");
                genericSFXPlayer.GetComponent<AudioSource>().PlayOneShot(noteAdded);
                inventoryManager.addNoteEntry(interactableObject.gameObject.GetComponent<FPEAttachedNote>().collectNote());
            }

        }


        private void retrievePassiveAudioDiary(GameObject interactableObject, bool wasInteractedWith)
        {

            if (interactableObject.gameObject.GetComponent<FPEPassiveAudioDiary>())
            {

                // Case 1: Player interacted with parent object, in which case we play in both autoplay and manual play scenarios
                // Case 2: Player just looked at parent object, so only play if autoplay is supported
                if ((wasInteractedWith == true) || ((wasInteractedWith == false) && (interactableObject.gameObject.GetComponent<FPEPassiveAudioDiary>().AutomaticPlayback == true)))
                {

                    if (interactableObject.gameObject.GetComponent<FPEPassiveAudioDiary>().HasBeenPlayed == false)
                    {

                        interactableObject.gameObject.GetComponent<FPEPassiveAudioDiary>().collectAudioDiary();
                        FPEAudioDiaryEntry tempEntry = new FPEAudioDiaryEntry(interactableObject.gameObject.GetComponent<FPEPassiveAudioDiary>().DiaryTitle, interactableObject.gameObject.GetComponent<FPEPassiveAudioDiary>().DiaryAudio, interactableObject.gameObject.GetComponent<FPEPassiveAudioDiary>().ShowDiaryTitle);
                        playAudioDiaryEntry(tempEntry);

                        if (interactableObject.gameObject.GetComponent<FPEPassiveAudioDiary>().AddEntryToInventory == true)
                        {
                            inventoryManager.addAudioDiaryEntry(tempEntry);
                        }

                    }

                }

            }

        }

        private void attemptActivation(GameObject objectToActivate)
        {

            FPEInteractableActivateScript activatee = objectToActivate.GetComponent<FPEInteractableActivateScript>();
            if (!activatee)
            {
                Debug.Log("FPEInteractionManagerScript.attemptActivation():: Given non-Activate type object called '"+objectToActivate.name+"'. No activation will take place.");
            }
            else
            {

                // Case 1: It requires inventory
                // OR
                // Case 2: It's a toggle, and it's toggled ON right now, AND toggling off requires inventory
                if ((activatee.RequireInventoryItem) || (activatee.EventFireType == FPEGenericEvent.eEventFireType.TOGGLE && activatee.IsToggledOn && activatee.ToggleOffRequiresInventory) )
                {

                    // Required in hand
                    if (activatee.RequiredToBeInHand == FPEInteractableActivateScript.eInventoryRequirementMode.IN_HAND)
                    {

                        if ((currentHeldObject != null && currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>() && currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>().InventoryItemType == activatee.RequiredItemType))
                        {

                            if (activatee.RemoveOnUse)
                            {
                                Destroy(currentHeldObject);
                                currentHeldObject = null;
                            }

                            currentInteractableObject.GetComponent<FPEInteractableActivateScript>().activate();

                        }
                        else
                        {
                            currentInteractableObject.GetComponent<FPEInteractableActivateScript>().failToActivate();
                        }

                    }
                    // Required in inventory
                    else if(activatee.RequiredToBeInHand == FPEInteractableActivateScript.eInventoryRequirementMode.IN_INVENTORY)
                    {

                        if (inventoryManager.inventoryQuantity(activatee.RequiredItemType) >= activatee.RequiredInventoryQuantity)
                        {

                            if (activatee.RemoveOnUse)
                            {
                                inventoryManager.destroyInventoryItemsOfType(activatee.RequiredItemType, activatee.RequiredInventoryQuantity);
                            }

                            currentInteractableObject.GetComponent<FPEInteractableActivateScript>().activate();

                        }
                        // Otherwise, we didn't have it, so react accordingly
                        else
                        {
                            currentInteractableObject.GetComponent<FPEInteractableActivateScript>().failToActivate();
                        }

                    }
                    // Can either be in hand or in inventory
                    else
                    {

                        // If it's in hand, do that first
                        if ((currentHeldObject != null && currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>() && currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>().InventoryItemType == activatee.RequiredItemType))
                        {

                            if (activatee.RemoveOnUse)
                            {
                                Destroy(currentHeldObject);
                                currentHeldObject = null;
                            }

                            currentInteractableObject.GetComponent<FPEInteractableActivateScript>().activate();

                        }
                        // Otherwise if it's in inventory, do that
                        else if (inventoryManager.inventoryQuantity(activatee.RequiredItemType) >= activatee.RequiredInventoryQuantity)
                        {

                            if (activatee.RemoveOnUse)
                            {
                                inventoryManager.destroyInventoryItemsOfType(activatee.RequiredItemType, activatee.RequiredInventoryQuantity);
                            }

                            currentInteractableObject.GetComponent<FPEInteractableActivateScript>().activate();

                        }
                        // Otherwise, cannot activate, do activation fail
                        else
                        {
                            currentInteractableObject.GetComponent<FPEInteractableActivateScript>().failToActivate();
                        }

                    }

                }
                else
                {
                    currentInteractableObject.GetComponent<FPEInteractableActivateScript>().activate();
                }

            }

        }


        private void showNotification(string message)
        {

            notificationLabel.GetComponent<Text>().color = defaultDiaryColor;
            notificationLabel.GetComponent<Text>().text = message;
            notificationCounter = notificationDuration;
            notificationLabelTargetScale = notificationLabelLargestScale;
            
        }

        #region CURSOR_AND_MOUSE

        // Sets mouse hints for LMB and RMB. If empty string passed in,
        // text and icon are set to invisible for associate LMB/RMB hint.
        private void setMouseHints(string zoomExamineHintText, string interactHintText, string inventoryHintText)
        {

            if (showMouseControlHints)
            {

                zoomExamineHint.setHint(zoomExamineHintText);
                interactHint.setHint(interactHintText);
                inventoryHint.setHint(inventoryHintText);

            }
            
        }

        
        private void updateControlHints()
        {

            FPEInteractableBaseScript.eInteractionType heldType = (currentHeldObject != null) ? currentHeldObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() : FPEInteractableBaseScript.eInteractionType.NULL_TYPE;
            FPEInteractableBaseScript.eInteractionType lookedAtType = (currentInteractableObject != null) ? currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() : FPEInteractableBaseScript.eInteractionType.NULL_TYPE;

            if (currentPutbackObject != null)
            {
                lookedAtType = FPEInteractableBaseScript.eInteractionType.PUT_BACK;
            }

            #region HINT_HOLDING_NOTHING
            if (heldType == FPEInteractableBaseScript.eInteractionType.NULL_TYPE)
            {

                switch (lookedAtType)
                {

                    case FPEInteractableBaseScript.eInteractionType.STATIC:
                        activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                        currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                        setMouseHints(mouseHintZoomText, "", "");
                        break;

                    case FPEInteractableBaseScript.eInteractionType.PICKUP:
                        activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                        currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                        setMouseHints(mouseHintZoomText, mouseHintPickUpText, "");
                        break;

                    case FPEInteractableBaseScript.eInteractionType.ACTIVATE:
                        activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                        currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                        setMouseHints(mouseHintZoomText, mouseHintActivateText, "");
                        break;

                    case FPEInteractableBaseScript.eInteractionType.JOURNAL:
                        activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                        currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                        setMouseHints(mouseHintZoomText, mouseHintJournalText, "");
                        break;

                    case FPEInteractableBaseScript.eInteractionType.AUDIODIARY:

                        if (currentInteractableObject.GetComponent<FPEInteractableAudioDiaryScript>().AutomaticPlayback == true)
                        {
                            activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                            setMouseHints(mouseHintExamineText, "", "");
                        }
                        else
                        {
                            activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                            setMouseHints(mouseHintExamineText, audioDiaryHint, "");
                        }

                        break;

                    case FPEInteractableBaseScript.eInteractionType.INVENTORY:
                        activateReticle(currentInteractableObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName);
                        currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                        if (currentInteractableObject.GetComponent<FPEInteractableInventoryItemScript>().PickupPermitted)
                        {
                            setMouseHints(mouseHintZoomText, mouseHintPickUpText, inventoryHintFromWorldPreText + " " + currentInteractableObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName);
                        }
                        else
                        {
                            setMouseHints(mouseHintZoomText, "", inventoryHintFromWorldPreText + " " + currentInteractableObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName);
                        }
                        
                        break;

                    case FPEInteractableBaseScript.eInteractionType.DOCK:
                        if (currentInteractableObject.GetComponent<FPEInteractableDockScript>().isOccupied() == false)
                        {
                            activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                            setMouseHints(mouseHintZoomText, currentInteractableObject.GetComponent<FPEInteractableDockScript>().DockHintText, "");
                        }
                        else
                        {
                            setMouseHints(mouseHintZoomText, "", "");
                        }
                        break;

                    // Holding Nothing, Looking at Nothing //
                    case FPEInteractableBaseScript.eInteractionType.PUT_BACK:
                    case FPEInteractableBaseScript.eInteractionType.NULL_TYPE:
                        deactivateReticle();
                        if (currentInteractableObject)
                        {
                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().unHighlightObject();
                        }
                        setMouseHints(mouseHintZoomText, "", "");
                        break;

                    default:
                        deactivateReticle();
                        setMouseHints(mouseHintZoomText, "", "");
                        Debug.LogError("FPEInteractionManagerScript:: Given bad eInteractionType '" + lookedAtType + "'.");
                        break;

                }

            }
            #endregion

            #region HINT_HOLDING_PICKUP
            else if (heldType == FPEInteractableBaseScript.eInteractionType.PICKUP)
            {

                switch (lookedAtType)
                {

                    case FPEInteractableBaseScript.eInteractionType.STATIC:
                        activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                        setMouseHints(mouseHintExamineText, mouseHintDropText, "");
                        break;

                    case FPEInteractableBaseScript.eInteractionType.PICKUP:
                        activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString, mouseHintPickUpHandsFullText);
                        setMouseHints(mouseHintExamineText, mouseHintDropText, "");
                        break;

                    case FPEInteractableBaseScript.eInteractionType.ACTIVATE:

                        if (currentInteractableObject.GetComponent<FPEInteractableActivateScript>().interactionsAllowedWhenHoldingObject())
                        {
                            activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                            setMouseHints(mouseHintExamineText, mouseHintActivateText, "");
                        }
                        else
                        {
                            activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString, activateTwoHandsHint);
                            setMouseHints(mouseHintExamineText, "", "");
                        }

                        break;

                    case FPEInteractableBaseScript.eInteractionType.JOURNAL:
                        activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString, journalTwoHandsHint);
                        currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                        setMouseHints(mouseHintExamineText, "", "");
                        break;

                    case FPEInteractableBaseScript.eInteractionType.AUDIODIARY:

                        if (currentInteractableObject.GetComponent<FPEInteractableAudioDiaryScript>().AutomaticPlayback == true)
                        {
                            activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                            setMouseHints(mouseHintExamineText, mouseHintDropText, "");
                        }
                        else
                        {
                            activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                            setMouseHints(mouseHintExamineText, audioDiaryHint, "");
                        }

                        break;

                    case FPEInteractableBaseScript.eInteractionType.INVENTORY:
                        // Special case: When holding something and looking at inventory, just show the name of the item, not "grab the item" since we can't pick it up right now.
                        activateReticle(currentInteractableObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName);
                        currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                        setMouseHints(mouseHintExamineText, mouseHintDropText, inventoryHintFromWorldPreText + " " + currentInteractableObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName);
                        break;

                    case FPEInteractableBaseScript.eInteractionType.DOCK:
                        if (currentInteractableObject.GetComponent<FPEInteractableDockScript>().isOccupied() == false)
                        {
                            activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                            setMouseHints(mouseHintZoomText, currentInteractableObject.GetComponent<FPEInteractableDockScript>().DockHintText, "");
                        }
                        else
                        {
                            setMouseHints(mouseHintZoomText, "", "");
                        }
                        break;

                    case FPEInteractableBaseScript.eInteractionType.PUT_BACK:
                        activateReticle(currentHeldObject.GetComponent<FPEInteractablePickupScript>().putBackString);
                        setMouseHints(mouseHintExamineText, mouseHintPutBackText, "");
                        break;

                    // Holding PICKUP, looking at nothing //
                    case FPEInteractableBaseScript.eInteractionType.NULL_TYPE:
                        deactivateReticle();
                        if (currentInteractableObject)
                        {
                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().unHighlightObject();
                        }
                        setMouseHints(mouseHintExamineText, mouseHintDropText, "");
                        break;

                    default:
                        deactivateReticle();
                        setMouseHints(mouseHintExamineText, mouseHintDropText, "");
                        Debug.LogError("FPEInteractionManagerScript:: Given bad eInteractionType '" + lookedAtType + "'.");
                        break;

                }

            }
            #endregion

            #region HINT_HOLDING_INVENTORY
            else if (heldType == FPEInteractableBaseScript.eInteractionType.INVENTORY)
            {

                switch (lookedAtType)
                {

                    case FPEInteractableBaseScript.eInteractionType.STATIC:
                        activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                        setMouseHints(mouseHintExamineText, mouseHintDropText, inventoryHintFromHandPreText + " " + currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName + " " + inventoryHintFromHandPostText);
                        break;

                    case FPEInteractableBaseScript.eInteractionType.PICKUP:
                        activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString, mouseHintPickUpHandsFullText);
                        setMouseHints(mouseHintExamineText, mouseHintDropText, inventoryHintFromHandPreText + " " + currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName + " " + inventoryHintFromHandPostText);
                        break;

                    case FPEInteractableBaseScript.eInteractionType.ACTIVATE:

                        if (currentInteractableObject.GetComponent<FPEInteractableActivateScript>().interactionsAllowedWhenHoldingObject())
                        {
                            activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                            setMouseHints(mouseHintExamineText, mouseHintActivateText, inventoryHintFromHandPreText + " " + currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName + " " + inventoryHintFromHandPostText);
                        }
                        else
                        {
                            activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString, activateTwoHandsHint);
                            setMouseHints(mouseHintExamineText, "", inventoryHintFromHandPreText + " " + currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName + " " + inventoryHintFromHandPostText);
                        }

                        break;

                    case FPEInteractableBaseScript.eInteractionType.JOURNAL:
                        activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString, journalTwoHandsHint);
                        currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                        setMouseHints(mouseHintExamineText, "", inventoryHintFromHandPreText + " " + currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName + " " + inventoryHintFromHandPostText);
                        break;

                    case FPEInteractableBaseScript.eInteractionType.AUDIODIARY:

                        if (currentInteractableObject.GetComponent<FPEInteractableAudioDiaryScript>().AutomaticPlayback == true)
                        {
                            activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                            setMouseHints(mouseHintExamineText, mouseHintDropText, inventoryHintFromHandPreText + " " + currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName + " " + inventoryHintFromHandPostText);
                        }
                        else
                        {
                            activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                            setMouseHints(mouseHintExamineText, audioDiaryHint, inventoryHintFromHandPreText + " " + currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName + " " + inventoryHintFromHandPostText);
                        }

                        break;

                    case FPEInteractableBaseScript.eInteractionType.INVENTORY:
                        // Special case: When inventory and looking at inventory, just show the name of the item, not "grab the item" since we can't pick it up right now.
                        activateReticle(currentInteractableObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName);
                        currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                        setMouseHints(mouseHintExamineText, mouseHintDropText, inventoryHintFromWorldPreText + " " + currentInteractableObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName);
                        break;

                    case FPEInteractableBaseScript.eInteractionType.DOCK:
                        if (currentInteractableObject.GetComponent<FPEInteractableDockScript>().isOccupied() == false)
                        {
                            activateReticle(currentInteractableObject.GetComponent<FPEInteractableBaseScript>().interactionString);
                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().highlightObject();
                            setMouseHints(mouseHintExamineText, currentInteractableObject.GetComponent<FPEInteractableDockScript>().DockHintText, inventoryHintFromHandPreText + " " + currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName + " " + inventoryHintFromHandPostText);
                        }
                        else
                        {
                            setMouseHints(mouseHintExamineText, "", inventoryHintFromHandPreText + " " + currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName + " " + inventoryHintFromHandPostText);
                        }
                        break;

                    case FPEInteractableBaseScript.eInteractionType.PUT_BACK:
                        setMouseHints(mouseHintExamineText, mouseHintDropText, inventoryHintFromHandPreText + " " + currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName + " " + inventoryHintFromHandPostText);
                        break;

                    // Holding INVENTORY, looking at nothing //
                    case FPEInteractableBaseScript.eInteractionType.NULL_TYPE:
                        deactivateReticle();
                        if (currentInteractableObject)
                        {
                            currentInteractableObject.GetComponent<FPEInteractableBaseScript>().unHighlightObject();
                        }
                        setMouseHints(mouseHintExamineText, mouseHintDropText, inventoryHintFromHandPreText + " " + currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName + " " + inventoryHintFromHandPostText);
                        break;

                    default:
                        deactivateReticle();
                        setMouseHints(mouseHintExamineText, mouseHintDropText, inventoryHintFromHandPreText + " " + currentHeldObject.GetComponent<FPEInteractableInventoryItemScript>().ItemName + " " + inventoryHintFromHandPostText);
                        Debug.LogError("FPEInteractionManagerScript:: Given bad eInteractionType '" + lookedAtType + "'.");
                        break;

                }

            }
            #endregion

            else
            {
                Debug.LogError("FPEInteractionManagerScript.updateControlHints():: Player is holding object of type '" + heldType + "' which is not implemented.");
            }

            if (currentDock)
            {
                unDockHint.setHint(currentDock.GetComponent<FPEInteractableDockScript>().UnDockHintText);
            }
            else
            {
                unDockHint.setHint("");
            }

            // Override case: If we're examining an object, clear the screen
            if (examiningObject)
            {
                setMouseHints("", "", "");
                unDockHint.setHint("");
            }

        }

        /// <summary>
        /// Creates and returns an array of strings based on current interactions. Can be used for debugging or other 
        /// UI outputs as required. Order is [currentInteractable.name, currentInteractble.type, currentPutBack.name, 
        /// PUT_BACK type, currentHeld.name, currentHeld.type]
        /// </summary>
        /// <returns>Array of strings containing alternating Name and Type for each of Current Interactable, Current Put Back, and Current Held Objects. If name strings are blank, the object was null.</returns>
        public string[] FetchCurrentInteractionDebugData()
        {

            string[] debugData = new string[6];

            debugData[0] = (currentInteractableObject != null) ? currentInteractableObject.name : "";
            debugData[1] = (currentInteractableObject != null) ? ""+currentInteractableObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() : ""+FPEInteractableBaseScript.eInteractionType.NULL_TYPE;
            debugData[2] = (currentPutbackObject != null) ? currentPutbackObject.name : "";
            debugData[3] = "" + FPEInteractableBaseScript.eInteractionType.PUT_BACK;
            debugData[4] = (currentHeldObject != null) ? currentHeldObject.name : "";
            debugData[5] = (currentHeldObject != null) ? "" + currentHeldObject.GetComponent<FPEInteractableBaseScript>().getInteractionType() : "" + FPEInteractableBaseScript.eInteractionType.NULL_TYPE;

            return debugData;

        }


        private void setCursorVisibility(bool visible)
        {

#if UNITY_5 || UNITY_2017
            Cursor.visible = visible;

            if (visible)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
#else
        Cursor.visible = visible;
		Screen.lockCursor = !visible;
#endif

        }

        // Note: This feature is only supported when using First Person Exploratino Kit with Unity 5 and above.
        // This function assumes that the top left pixel is the hotspot. To change that, simply pass in the hotspot
        // location rather than Vector2.zero.
        private void setCursorTexture(Texture2D cursorTex)
        {

#if UNITY_5 || UNITY_2017
            Cursor.SetCursor(cursorTex, Vector2.zero, CursorMode.ForceSoftware);
#endif

        }
        #endregion

        /// <summary>
        /// Hides all UI elements
        /// </summary>
        public void hideAllUI()
        {

            zoomExamineHint.setHintVisibility(false);
            interactHint.setHintVisibility(false);
            inventoryHint.setHintVisibility(false);
            unDockHint.setHintVisibility(false);
            hideReticleAndInteractionLabel();

        }

        /// <summary>
        /// Unhides all UI elements to previous pre-hidden state. They are refreshed in the next frame.
        /// </summary>
        public void unhideAllUI()
        {

            zoomExamineHint.setHintVisibility(true);
            interactHint.setHintVisibility(true);
            inventoryHint.setHintVisibility(true);
            unDockHint.setHintVisibility(true);
            showReticleAndInteractionLabel();
            // Lastly, update control hints in case they were made stale while hidden
            updateControlHints();

        }

        private void OnApplicationFocus(bool hasFocus)
        {

            // We have focus and the menu is not open and we don't have cursor lock, so ask for cursor lock
            if (hasFocus && !menuOpen && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Customize the body of these functions as required for your Character Controller code of choice. If using something //
        // like UFPS, you may want to overhaul these completely. If you need help, please email support@whilefun.com          //
        //                                                                                                                    //
        // Note: The Dock-related functions may require non-trivial changes to your player controller scripts. To see how you //
        //       can implement this fucntionality in your own scripts, please refer to FPEFirstPersonController.cs            //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region PLAYER_CONTROLLER_SPECIFIC

        // This function records our starting sensitivity.
        // The Standard Asset version of the MouseLook script uses X on Character Controller and Y on Camera.
        private void rememberStartingMouseSensitivity()
        {
            startingMouseSensitivity = FPEInputManager.Instance.LookSensitivity;
        }
        public void refreshAlternateMouseSensitivities()
        {
            zoomedMouseSensitivity = startingMouseSensitivity * zoomedMouseSensitivityMultiplier;
            highlightedMouseSensitivity = startingMouseSensitivity * highlightedMouseSensitivityMultiplier;
        }
        // Set sensitivity directly, and ensure smoothMouseChange is off.
        private void setMouseSensitivity(Vector2 sensitivity)
        {
            FPEInputManager.Instance.LookSensitivity = sensitivity;
            smoothMouseChange = false;
        }
        // Restores mouse sensitivity to starting Mouse sensitivity
        // Vector2 is desired sensitivity. If smoothTransition is true, sensitivity 
        // change is gradual. Otherwise, it is changed immediately.
        private void restorePreviousMouseSensitivity(bool smoothTransition)
        {

            if (smoothTransition)
            {
                targetMouseSensitivity.x = startingMouseSensitivity.x;
                targetMouseSensitivity.y = startingMouseSensitivity.y;
                smoothMouseChange = true;
            }
            else
            {
                FPEInputManager.Instance.LookSensitivity = startingMouseSensitivity;
                smoothMouseChange = false;
            }

        }
        // A hook for a menu or UI to set the mouse sensitivity during the game. Note in this case, both X and Y are set to the
        // same value to simplify the UI. This can be done a different way, respecting X and Y as separate values if desired.
        public void changeMouseSensitivityFromMenu(float sensitivity)
        {
            FPEInputManager.Instance.LookSensitivity = new Vector2(sensitivity, sensitivity);
            rememberStartingMouseSensitivity();
            refreshAlternateMouseSensitivities();
            smoothMouseChange = false;
        }
        // Locks mouse look, so we can move mouse to rotate objects when examining them.
        // If using another Character Controller (UFPS, etc.) substitute mouselook disable functionality
        private void disableMouseLook()
        {
            thePlayer.GetComponent<FPEMouseLook>().enableMouseLook = false;
            mouseLookEnabled = false;
        }
        // Unlocks mouse look so we can move mouse to look when walking/moving normally.
        // If using another Character Controller (UFPS, etc.) substitute mouselook enable functionality
        private void enableMouseLook()
        {
            thePlayer.GetComponent<FPEMouseLook>().enableMouseLook = true;
            mouseLookEnabled = true;
        }
        // Locks movement of Character Controller. 
        // If using another Character Controller (UFPS, etc.) substitute disable functionality
        private void disableMovement()
        {
            thePlayer.GetComponent<FPEFirstPersonController>().disableMovement();
        }
        // Unlocks movement of Character Controller. 
        // If using another Character Controller (UFPS, etc.) substitute enable functionality
        private void enableMovement()
        {
            thePlayer.GetComponent<FPEFirstPersonController>().enableMovement();
        }

        public bool isMouseLookEnabled()
        {
            return mouseLookEnabled;
        }

        /// <summary>
        /// Starts docking the player to the specified dock
        /// </summary>
        /// <param name="dock"></param>
        private void DockPlayer(FPEInteractableDockScript dock)
        {

            currentDock = dock.gameObject;
            dock.dock();
            unDockHint.setHint(dock.UnDockHintText);
            thePlayer.GetComponent<FPEFirstPersonController>().dockThePlayer(dock.DockTransform, dock.DockedViewLimits, dock.FocusTransform.position, dock.SmoothDock);
            currentDockActionType = FPEFirstPersonController.ePlayerDockingState.DOCKING;
            dockingInProgress = true;
            hideAllUI();

        }

        /// <summary>
        /// Starts to Un-Dock the player from their current Dock. 
        /// Note: converting this function to work with assets such as UFPS may be a non-trivial exercise. See FPEFirstPersonController.cs for details on existing implementation.
        /// </summary>
        /// <param name="smoothDock">If true, player view and position will smoothly lerp based on FPEFirstPersonController's Docking Lerp Factor value. Defaults to false.</param>
        private void UnDockPlayer(bool smoothDock = false)
        {

            thePlayer.GetComponent<FPEFirstPersonController>().unDockThePlayer(smoothDock);
            currentDock.GetComponent<FPEInteractableDockScript>().unDock();
            unDockHint.setHint("");
            currentDockActionType = FPEFirstPersonController.ePlayerDockingState.UNDOCKING;
            dockingInProgress = true;
            hideAllUI();

        }

        /// <summary>
        /// Checks with player controller to see if dock is currently in progress
        /// </summary>
        /// <returns>True if docking is in progress, false if it is not</returns>
        private bool dockingCompleted()
        {
            return !thePlayer.GetComponent<FPEFirstPersonController>().dockInProgress();
        }

        /// <summary>
        /// For use by Save Load Manager.
        /// </summary>
        /// <returns>the currentDock (can be null)</returns>
        public GameObject getCurrentDockForSaveGame()
        {
            return currentDock;
        }

        /// <summary>
        /// For use by Save Load Manager only.
        /// </summary>
        /// <param name="cd">Gameobject to assign to current dock (can be null)</param>
        public void restoreCurrentDockFromSavedGame(GameObject cd)
        {

            currentDock = cd;
            dockingInProgress = false;

            if (cd != null)
            {
                unDockHint.setHint(currentDock.GetComponent<FPEInteractableDockScript>().UnDockHintText);
            }

        }

        /// <summary>
        /// Restricts player's look ability to be +/- specified x and y angles, relative to current reticle position.
        /// </summary>
        /// <param name="angles">The angles or bounds that will limit player view, +/-</param>
        public void RestrictPlayerLookFromCurrentView(Vector2 angles)
        {
            thePlayer.GetComponent<FPEMouseLook>().enableLookRestriction(angles);
        }

        /// <summary>
        /// Removes any existing view restriction on player's view.
        /// </summary>
        public void FreePlayerLookFromCurrentViewRestrictions()
        {
            thePlayer.GetComponent<FPEMouseLook>().disableLookRestriction();
        }

        /// <summary>
        /// This function disables player movement and look, for use by SaveLoadManager or other operation that requires player be 'locked'
        /// </summary>
        public void suspendPlayerAndInteraction()
        {

            playerSuspendedForSaveLoad = true;
            disableMouseLook();
            disableMovement();

            thePlayer.GetComponent<FPEFirstPersonController>().playerFrozen = true;

            menuOpen = true;
            setCursorVisibility(true);

            hideAllUI();

        }

        /// <summary>
        /// This function enables player movement and look, for use by SaveLoadManager or other operation when it no longer requires player be 'locked'
        /// </summary>
        public void resumePlayerAndInteraction(bool resetLook)
        {

            if (resetLook)
            {
                FPEPlayer.Instance.GetComponent<FPEFirstPersonController>().setPlayerLookToNeutralLevelLoadedPosition();
            }

            playerSuspendedForSaveLoad = false;

            enableMouseLook();
            enableMovement();

            thePlayer.GetComponent<FPEFirstPersonController>().playerFrozen = false;

            menuOpen = false;
            setCursorVisibility(false);

            unhideAllUI();

        }

        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    }

}