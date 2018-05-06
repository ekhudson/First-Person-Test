using System;
using UnityEngine;
using Whilefun.FPEKit;

//
// DemoCabinetScript
// This script handles the core state management and animations for
// the cabinet.
//
// Copyright 2017 While Fun Games
// http://whilefun.com
//
public class DemoCabinetScript : FPEGenericSaveableGameObject {

	private bool cabinetOpen = false;
	private GameObject doorOpenerLeft = null;
	private GameObject doorOpenerRight = null;

	void Awake(){

        doorOpenerLeft = transform.Find("LeftDoor/DoorOpenerLeft").gameObject;
        doorOpenerRight = transform.Find("RightDoor/DoorOpenerRight").gameObject;

    }

	public void openCabinet()
    {

		doorOpenerLeft.GetComponent<FPEInteractableActivateScript>().interactionString = "Close cabinet";
		doorOpenerRight.GetComponent<FPEInteractableActivateScript>().interactionString = "Close cabinet";
		gameObject.GetComponent<Animator>().SetTrigger("OpenCabinet");
        gameObject.GetComponent<Animator>().ResetTrigger("CloseCabinet");
        gameObject.GetComponent<Animator>().SetBool("ForceOpenCabinet", false);

    }

	public void closeCabinet()
    {

		doorOpenerLeft.GetComponent<FPEInteractableActivateScript>().interactionString = "Open cabinet";
		doorOpenerRight.GetComponent<FPEInteractableActivateScript>().interactionString = "Open cabinet";
		gameObject.GetComponent<Animator>().SetTrigger("CloseCabinet");
        gameObject.GetComponent<Animator>().ResetTrigger("OpenCabinet");
        gameObject.GetComponent<Animator>().SetBool("ForceOpenCabinet", false);

    }

	public void setCabinetOpen()
    {
		cabinetOpen = true;
	}

	public void setCabinetClosed()
    {
		cabinetOpen = false;
	}

	public bool isCabinetOpen()
    {
		return cabinetOpen;
	}

    public override FPEGenericObjectSaveData getSaveGameData()
    {
        return new FPEGenericObjectSaveData(gameObject.name, 0, 0f, cabinetOpen);
    }

    public override void restoreSaveGameData(FPEGenericObjectSaveData data)
    {

        cabinetOpen = data.SavedBool;

        if (cabinetOpen)
        {

            doorOpenerLeft.GetComponent<FPEInteractableActivateScript>().interactionString = "Close cabinet";
            doorOpenerRight.GetComponent<FPEInteractableActivateScript>().interactionString = "Close cabinet";
            gameObject.GetComponent<Animator>().SetBool("ForceOpenCabinet", true);
            gameObject.GetComponent<Animator>().ResetTrigger("CloseCabinet");
            gameObject.GetComponent<Animator>().ResetTrigger("OpenCabinet");

        }

    }

}
