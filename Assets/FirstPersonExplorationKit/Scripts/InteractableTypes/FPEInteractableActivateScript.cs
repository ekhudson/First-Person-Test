using UnityEngine;

namespace Whilefun.FPEKit
{

    //
    // FPEInteractableActivateScript
    // This script is the basis for all Activate type Interactable
    // objects. This should never be assigned to a game object in
    // your scene, but instead used as a base class for objects that
    // can be activated. See Demo scripts for examples on how to make
    // your own activate child type objects.
    //
    // Copyright 2017 While Fun Games
    // http://whilefun.com
    //
    public class FPEInteractableActivateScript : FPEInteractableBaseScript
    {

        [SerializeField, Tooltip("If true, player can interact with this while holding something. If false, they cannot.")]
        protected bool canInteractWithWhileHoldingObject = true;

        [Header("Inventory Item Requirements (Optional)")]
        [SerializeField, Tooltip("If true, activation will require an inventory item with the parameters as configured below")]
        private bool requireInventoryItem = false;
        public bool RequireInventoryItem { get { return requireInventoryItem; } }

        [SerializeField, Tooltip("The type of inventory item required to activate")]
        private FPEInventoryManagerScript.eInventoryItems requiredItemType = FPEInventoryManagerScript.eInventoryItems.APPLE;
        public FPEInventoryManagerScript.eInventoryItems RequiredItemType { get { return requiredItemType; } }

        [SerializeField, Tooltip("The quantity of inventory item required for the activation")]
        private int requiredInventoryQuantity = 1;
        public int RequiredInventoryQuantity {  get { return requiredInventoryQuantity; } }

        public enum eInventoryRequirementMode
        {
            IN_HAND = 0,
            IN_INVENTORY = 1,
            IN_HAND_OR_INVENTORY = 2 
        }

        [SerializeField, Tooltip("If true, the inventory item must be Held (in player's hand) for activation (e.g. wrench). If false, can just be in inventory (e.g. secret password).")]
        private eInventoryRequirementMode requiredToBeInHand = eInventoryRequirementMode.IN_HAND_OR_INVENTORY;
        public eInventoryRequirementMode RequiredToBeInHand {  get { return requiredToBeInHand; } }

        [SerializeField, Tooltip("If true, the activation will remove the item(s) once activation is triggered. (e.g. Remove battery when placed in radio. Don't remove keycard when swiped to open door.")]
        private bool removeOnUse = false;
        public bool RemoveOnUse {  get { return removeOnUse; } }

        [Header("Type of Activation and Events")]
        [SerializeField, Tooltip("This will dictate the nature of the events. ONCE: Will only fire the first time. EVERYTIME: Will fire on every activation.")]
        private FPEGenericEvent.eEventFireType eventFireType = FPEGenericEvent.eEventFireType.ONCE;
        public FPEGenericEvent.eEventFireType EventFireType {  get { return eventFireType; } }

        [Header("Shared 'Activation' and 'Toggle On' Event")]
        [SerializeField, Tooltip("If specified, this event will fire on activation. It will adhere to defined occurrence type (e.g. fire once, fire every time, etc.)")]
        private FPEGenericEvent ActivationEvent;

        [Header("Shared 'Activation Failure' Event")]
        [SerializeField, Tooltip("If specified, this event is fired when the player needs inventory to activate the object but does not have it. (e.g. beep and flash a 'keycard required' warning light)")]
        private FPEGenericEvent ActivationFailureEvent;

        [Header("TOGGLE-specific 'Toggle Off' Event")]
        [SerializeField, Tooltip("If specified, this event will fire on de-activation or 'toggle off'.")]
        private FPEGenericEvent DeactivationEvent;

        [SerializeField, Tooltip("If true, toggling object OFF will also adhere to inventory requirements above")]
        private bool toggleOffRequiresInventory = false;
        public bool ToggleOffRequiresInventory { get { return toggleOffRequiresInventory; } }

        [SerializeField, Tooltip("Overrides interaction string, and applied when toggle is currently OFF")]
        private string toggleOnInteractionString;
        [SerializeField, Tooltip("Overrides interaction string, and applied when toggle is currently ON")]
        private string toggleOffInteractionString;

        [SerializeField, Tooltip("If true, the toggle will toggle ON and fire the ToggleOnEvent when scene starts.")]
        private bool toggleOnAtStart = false;
        [SerializeField, Tooltip("If true, the toggle will toggle OFF and fire the ToggleOnEvent when scene starts.")]
        private bool toggleOffAtStart = false;

        [Header("Save Game Options")]
        [SerializeField, Tooltip("If Event Fire Type is TOGGLE, and this value is true, the Toggle On and Toggle Off events will re-occur when game is loaded. For some objects like lights, a value of true is advised. For other objects like doors, a value of false makes more sense.")]
        private bool fireToggleEventsOnLoadGame = true;

        private bool toggleOn = false;
        public bool IsToggledOn { get { return toggleOn; } }

        private bool eventHasFiredOnce = false;
        
        public override void Awake()
        {

            base.Awake();
            interactionType = eInteractionType.ACTIVATE;
            // You may want to override assigned Inspector value for canInteractWithWhileHoldingObject in child classes, depending on the object and use case

            if (requireInventoryItem && requiredInventoryQuantity > 1 && requiredToBeInHand == eInventoryRequirementMode.IN_HAND)
            {
                Debug.LogError("FPEInteractableActivateScript:: Object '"+gameObject.name+"' is configured to require more than one '"+requiredItemType+"' but also that the '"+ requiredItemType + "'s are in player's hand. Player can only activate with one object in hand at a time. This activation will not work correctly.", gameObject);
            }
            else if (requireInventoryItem && requiredInventoryQuantity > 1 && requiredToBeInHand == eInventoryRequirementMode.IN_HAND_OR_INVENTORY)
            {
                Debug.LogWarning("FPEInteractableActivateScript:: Object '" + gameObject.name + "' is configured to require more than one '" + requiredItemType + "' but also that the '" + requiredItemType + "'s are in either in player's hand or in inventory. Player can only activate with one object in hand at a time. This activation will may work as expected.", gameObject);
            }

            if(toggleOnAtStart && toggleOffAtStart)
            {
                Debug.LogWarning("FPEInteractableActivateScript:: Object '" + gameObject.name + "' has both Toggle On At Start and Toggle Off At Start set to true.");
            }

        }

        public override void Start()
        {

            base.Start();

            // Some sanity checks if the wrong checkboxes are checked
            if (eventFireType == FPEGenericEvent.eEventFireType.ONCE || eventFireType == FPEGenericEvent.eEventFireType.EVERYTIME)
            {

                if(DeactivationEvent.GetPersistentEventCount() != 0)
                {
                    Debug.LogWarning("Object '"+gameObject.name+"' is of fire type '"+ eventFireType + "', but has a DeactivationEvent specified. This event will not fire unless you use TOGGLE type.", gameObject);
                }

            }
            else if (eventFireType == FPEGenericEvent.eEventFireType.TOGGLE)
            {
                if (DeactivationEvent.GetPersistentEventCount() == 0)
                {
                    Debug.LogWarning("Object '" + gameObject.name + "' is of fire type 'TOGGLE', but has no DeactivationEvent specified. Consider using a different type.");
                }
            }

            if (eventFireType == FPEGenericEvent.eEventFireType.TOGGLE)
            {

                if (toggleOnAtStart)
                {
                    doToggleOn();
                }
                if(toggleOffAtStart)
                {
                    doToggleOff();
                }

            }

        }

        public override bool interactionsAllowedWhenHoldingObject()
        {
            return canInteractWithWhileHoldingObject;
        }

        public virtual void activate()
        {

            base.interact();

            if (eventFireType == FPEGenericEvent.eEventFireType.ONCE)
            {

                if (!eventHasFiredOnce)
                {

                    eventHasFiredOnce = true;

                    if (ActivationEvent != null)
                    {
                        ActivationEvent.Invoke();
                    }

                }

            }
            else if (eventFireType == FPEGenericEvent.eEventFireType.EVERYTIME)
            {

                if (ActivationEvent != null)
                {
                    ActivationEvent.Invoke();
                }

            }
            else if (eventFireType == FPEGenericEvent.eEventFireType.TOGGLE)
            {

                if (toggleOn)
                {
                    doToggleOff();
                }
                else
                {
                    doToggleOn();
                }

            }
            else
            {
                Debug.LogWarning("FPEInteractableActivateGenericScript:: eventFireType '" + eventFireType + "' not yet implemented!");
            }


        }

        public void failToActivate()
        {

            if(ActivationFailureEvent != null)
            {
                ActivationFailureEvent.Invoke();
            }

        }

        private void doToggleOn()
        {

            toggleOn = true;
            interactionString = toggleOffInteractionString;

            if (ActivationEvent != null)
            {
                ActivationEvent.Invoke();
            }

        }

        private void doToggleOff()
        {

            toggleOn = false;
            interactionString = toggleOnInteractionString;

            if (DeactivationEvent != null)
            {
                DeactivationEvent.Invoke();
            }

        }

        #region GAME_SAVE_LOAD

        public FPEActivateSaveData getSaveGameData()
        {
            return new FPEActivateSaveData(gameObject.name, eventHasFiredOnce, toggleOn, interactionString);
        }

        public void restoreSaveGameData(FPEActivateSaveData data)
        {

            eventHasFiredOnce = data.FiredOnce;
            toggleOn = data.ToggleCurrentlyOn;
            interactionString = data.InteractionString;

            if (eventFireType == FPEGenericEvent.eEventFireType.TOGGLE && fireToggleEventsOnLoadGame)
            {

                if (toggleOn)
                {
                    doToggleOn();
                }
                else
                {
                    doToggleOff();
                }

            }

        }

        #endregion

    }

}