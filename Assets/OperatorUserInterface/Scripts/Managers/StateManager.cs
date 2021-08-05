﻿using System.Collections.Generic;
using Training;
using UnityEngine;
using UnityEngine.SceneManagement;
//using Valve.VR;
using UnityEngine.Events;

public class StateManager : Singleton<StateManager>
{
    //For debugging: if true, disables all construct functionality in this script
    public bool KillConstruct;
    public States currentState;

    [SerializeField] private ClientLogic clientLogic;

    AdditiveSceneManager additiveSceneManager;
    ConstructFXManager constructFXManager;
    TransitionManager transitionManager;
    GameObject leftSenseGlove;
    GameObject rightSenseGlove;
    BioIK.BioIK[] bioIks;

    List<StateManager.States> visitedStates = new List<States>();
    /// <summary>
    /// The states the operator can be in
    /// </summary>
    public enum States
    {
        HUD, Construct, Training
    }

    /// <summary>
    /// Set reference to instances.
    /// Load construct as initial state.
    /// </summary>
    void Start()
    {
        constructFXManager = GameObject.FindGameObjectWithTag("ConstructFXManager").GetComponent<ConstructFXManager>();
        additiveSceneManager = GameObject.FindGameObjectWithTag("AdditiveSceneManager").GetComponent<AdditiveSceneManager>();
        transitionManager = GameObject.FindGameObjectWithTag("TransitionManager").GetComponent<TransitionManager>();

        leftSenseGlove = GameObject.FindGameObjectWithTag("SenseGloveLeft");
        rightSenseGlove = GameObject.FindGameObjectWithTag("SenseGloveRight");
        leftSenseGlove.SetActive(false);
        rightSenseGlove.SetActive(false);

        bioIks = FindObjectsOfType<BioIK.BioIK>();

        //additiveSceneManager.ChangeScene(Scenes.CONSTRUCT, null, null, DelegateBeforeConstructLoad, DelegateAfterConstructLoad);
        //currentState = States.Construct;
        additiveSceneManager.ChangeScene(Scenes.TRAINING, null, null, null, DelegateAfterTrainingLoad);
        currentState = States.Training;
        visitedStates.Add(States.Training);
    }

    /// <summary>
    /// Public method initiating state transition.
    /// Change from current state to next state (fixed order).
    /// </summary>
    public void GoToNextState()
    {
        switch (currentState)
        {
            case States.HUD:
                GoToState(States.Training);
                break;
            case States.Construct:
                GoToState(States.HUD);
                break;
            case States.Training:
                GoToState(States.HUD);
                break;
            default:
                Debug.LogWarning("Unhandled State: Please specify the next State after " + currentState);
                break;
        }
    }

    /// <summary>
    /// Load the specified state.
    /// </summary>
    /// <param name="newState">The name of the state the state that should be loaded.</param>
    /// <param name="onLoadDone">Callback to be executed once scene switching is done</param>
    public void GoToState(States newState, System.Action onLoadDone = null)
    {
        // TODO: not working because the wheelchair is overwriting the position but needed to reset the user
        //WheelchairStateManager.Instance.transform.position = Vector3.zero;
        
        // keep motor disabled at all times, only HUD can send motor commands which is set in the DelegateAfterHudLoad
        clientLogic.unityClient.EnableMotor(false);

        switch (newState)
        {
            case States.Construct:
                transitionManager.StartTransition(false);
                additiveSceneManager.ChangeScene(Scenes.CONSTRUCT, null, null, DelegateBeforeConstructLoad, () =>
                {
                    DelegateAfterConstructLoad();
                    onLoadDone?.Invoke();
                });
                currentState = States.Construct;
                break;
            case States.HUD:
                transitionManager.StartTransition(true);
                //additiveSceneManager.ChangeScene(Scenes.HUD, null, null, DelegateBeforeHudLoad, () =>
                additiveSceneManager.ChangeScene(Scenes.HUD, null, null, DelegateBeforeHudLoad, () =>
                {
                    DelegateAfterHudLoad();
                    onLoadDone?.Invoke();
                });
                currentState = States.HUD;
                break;
            case States.Training:
                transitionManager.StartTransition(true);
                //additiveSceneManager.ChangeScene(Scenes.TRAINING, null, null, null, () =>
                additiveSceneManager.ChangeScene(Scenes.TRAINING, null, null, DelegateBeforeTrainingLoad, () =>
                {
                    DelegateAfterTrainingLoad();
                    onLoadDone?.Invoke();
                });
                currentState = States.Training;
                break;
            default:
                Debug.LogWarning("Unhandled State: Please specify the next State after " + currentState);
                break;
        }
        visitedStates.Add(currentState);
    }
    public int TimesStateVisited(StateManager.States state)
    {
        return visitedStates.FindAll(x => x == state).Count;
    }

    /// <summary>
    /// For debugging.
    /// Initiate transition to next state by pressing space on your keyboard while in play mode in Unity.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            GoToNextState();
        if (Input.GetKeyDown(KeyCode.T) && currentState == States.Construct)
            GoToState(States.Training);
    }

    #region Delegates
    /// <summary>
    /// Logic that is executed right before the construc scene is loaded.
    /// Enables both sense gloves and the OpenMenuButton
    /// </summary>
    void DelegateBeforeConstructLoad()
    {
        if (!KillConstruct)
        {
            leftSenseGlove.SetActive(true);
            rightSenseGlove.SetActive(true);

            Transform openMenuButton = leftSenseGlove.transform.GetChild(1);
            openMenuButton.gameObject.SetActive(true);
            openMenuButton.GetChild(0).GetComponent<ButtonRigidbodyConstraint>().InitialState();
            openMenuButton.GetChild(1).GetComponent<FrameClickDetection>().highlightOff();
        }
    }

    /// <summary>
    /// Logic that is executed right before the Trainings scene is loaded.
    /// Gets the reference to the TutorialSteps script.
    /// </summary>
    void DelegateAfterTrainingLoad()
    {
        TutorialSteps.Instance = GameObject.FindGameObjectWithTag("Tutorial").GetComponent<TutorialSteps>();
    }

    /// <summary>
    /// Logic that is executed right after the construct scene has been loaded.
    /// Set roboy model to the position of the operator.
    /// Hook up quest button (what to do if pressed).
    /// Attach menu to the camera.
    /// Activate construct visual effects.
    /// </summary>
    void DelegateAfterConstructLoad()
    {
        if (!KillConstruct)
        {
            Transform cameraOrigin = GameObject.FindGameObjectWithTag("CameraOrigin").transform;
            Transform constructObjects = GameObject.FindGameObjectWithTag("ConstructObjects").transform;
            GameObject roboy = GameObject.FindGameObjectWithTag("Roboy");

            if (roboy != null)
            {
                roboy.transform.SetParent(GameObject.FindGameObjectWithTag("FinalScenePlaceholder").transform);
                roboy.transform.position = cameraOrigin.position + new Vector3(0f, 1.4f, 0f);
                roboy.transform.localEulerAngles = cameraOrigin.transform.localEulerAngles;//Quaternion.Euler(roboy.rotation.eulerAngles + cameraOrigin.rotation.eulerAngles);//cameraOrigin.GetChild(1).rotation;//roboy.rotation * cameraOrigin.GetChild(1).rotation;
            }

            FrameClickDetection questButton = constructObjects.GetChild(0).GetComponentInChildren<FrameClickDetection>();
            questButton.onPress[0].AddListener(GameObject.FindGameObjectWithTag("FinalsDemoScriptManager").GetComponent<FinalsDemoScriptManager>().StartQuest);
            questButton.onPress[1].AddListener(GameObject.FindGameObjectWithTag("FinalsDemoScriptManager").GetComponent<FinalsDemoScriptManager>().StopQuest);
            constructObjects.GetChild(0).SetParent(cameraOrigin, false);

            constructFXManager.ToggleEffects(true);
        }
    }

    /// <summary>
    /// Logic that is executed after the construct scene has been destroyed.
    /// Destroy the menu, which belongs to the construct scene but was attached to the camera (final scene).
    /// Release sense gloves in case they were interacting when the transition was initiated.
    /// Disable Sense gloves.
    /// Deactivate construct visual effects.
    /// </summary>
    void DelegateOnConstructUnload()
    {
        if (!KillConstruct)
        {
            Destroy(GameObject.FindGameObjectWithTag("MainMenu"));
            //#if SENSEGLOVE
            //            leftSenseGlove.GetComponentInChildren<SenseGlove_Object>().StopBrakes();
            //            rightSenseGlove.GetComponentInChildren<SenseGlove_Object>().StopBrakes();
            //            leftSenseGlove.SetActive(false);
            //            rightSenseGlove.SetActive(false);
            //#endif

            constructFXManager.ToggleEffects(false);
        }
    }

    /// <summary>
    /// Logic that is executed right before the construc scene is loaded.
    /// Enables both sense gloves and the OpenMenuButton
    /// </summary>
    void DelegateBeforeHudLoad()
    {
        print("DelegateBeforeHudLoad");

        // reset all joints to 0 before going to HUD
        if (TimesStateVisited(States.HUD) == 0)
        {
            foreach (var body in bioIks)
            {

                // switch to instantaneous movement type for BioIK so that the transition to joint targets 0 is immediate 
                body.MotionType = BioIK.MotionType.Instantaneous;

                foreach (var segment in body.Segments)
                {
                    body.ResetPosture(segment);
                }


            }
        }
        else
        {
            // go to the latest joint targets before leaving HUD
            var jointValues = clientLogic.unityClient.GetLatestJointValues();
            if (jointValues.Count > 0)
            {
                int i = 0;
                foreach (var body in bioIks)
                {
                    if (body.name.Contains("shadow"))
                    {
                        continue;
                    }
                    foreach (var segment in body.Segments)
                    {
                        if (segment.Joint != null)
                        {
                            //Debug.Log($"{body.name}: {segment.Joint.name} {i}");
                            segment.Joint.X.SetTargetValue(jointValues[i]);

                            i++;
                        }
                    }
                }
            }
        }
        Debug.Log("Presense indicator on");
        clientLogic.unityClient.SetPresenceIndicatorOn(true);

    }

    void DelegateAfterHudLoad()
    {
        Debug.Log("DelegaterAfterHudLoad");
        // switch back to realistic (slow) motion and enable motor
        foreach (var body in bioIks)
        {
            body.MotionType = BioIK.MotionType.Realistic;
        }
        clientLogic.unityClient.EnableMotor(true);
    }

    void DelegateOnHudUnload()
    {
        Debug.Log("DelegateOnHudUnload");
    }

    void DelegateBeforeTrainingLoad()
    {
        Debug.Log("Presense indicator off");
        clientLogic.unityClient.SetPresenceIndicatorOn(false);
        
    }
    #endregion
}