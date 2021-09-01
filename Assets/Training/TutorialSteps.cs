﻿using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Widgets;
using System;

namespace Training
{
    public class TutorialSteps : Automaton<TutorialSteps.TrainingStep>
    {
        public enum TrainingStep
        {
            IDLE,
            HEAD,
            LEFT_ARM,
            LEFT_HAND,
            RIGHT_ARM,
            RIGHT_HAND,
#if SENSEGLOVE
            ARM_LENGTH,
#endif
            WHEELCHAIR,
            PAUSE_MENU,
            DONE
        }

        public static TutorialSteps Instance;

        public AudioManager audioManager;

        public AudioClips.Misc miscAudio;
        public AudioClips.SGTraining senseGloveAudio;
        public AudioClips.Controller controllerAudio;

        public List<AudioClip> praisePhrases = new List<AudioClip>();

        public bool waitingForNod = false;
        public Calibration.HandCalibrator rightCalibrator, leftCalibrator;
        public Calibration.ArmLength.ArmLength armLengthCalibration;
        public WheelchairTraining wheelChairTraining;
        public PauseMenuTraining pauseMenuTraining;
        

        //int toggle;
        //double prevDuration = 0.0;
        //double prevStart = 0.0;
        TrainingStep lastCorrectedAtStep = TrainingStep.IDLE;
        private bool waitStarted = false, startTraining = true;


        [SerializeField] private Transform handCollectables;

        [SerializeField] private GameObject designatedArea;

        private IEnumerator StartTrainingAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            // audioManager.ScheduleAudioClip(miscAudio.welcome, queue: false,
            //    onStart: () => PublishNotification("Welcome to Teleport VR!", miscAudio.welcome.length)
            //    );
            audioManager.ScheduleAudioClip(miscAudio.imAria, queue: true,
                onStart: () => PublishNotification("I am Aria - your personal telepresence trainer.", miscAudio.imAria.length + 2),
                onEnd: () =>
                {
                    currentState = TrainingStep.IDLE;
                    Debug.Log("Started Training");
                    Next();
                    waitStarted = false;
                    startTraining = true;
                }
            );

        }

        void Start()
        {
            Debug.Log(StateManager.Instance.TimesStateVisited(StateManager.States.Training));
            // get a reference to this singleton, as scripts from other scenes are not able to do this
            _ = Instance;
            if (StateManager.Instance.TimesStateVisited(StateManager.States.Training) <= 1)
            {

                waitStarted = true;
                StartCoroutine(StartTrainingAfter(0));
            }
            else
            {
                Debug.Log("Training routine skipped.");
                startTraining = false;
            }
            //currentStep = TrainingStep.RIGHT_HAND;
            //NextStep();
            //trainingStarted = false;

#region StateDefinition

            stateMachine.onEnter[TrainingStep.HEAD] = (step) =>
            {
                audioManager.ScheduleAudioClip(miscAudio.head,
                    onStart: () => PublishNotification("Try moving your head around", miscAudio.head.length)
                    );
                audioManager.ScheduleAudioClip(miscAudio.nod, queue: true,
                    onStart: () => PublishNotification("Give me a nod to continue", miscAudio.nod.length + 2)
                    );
                waitingForNod = true;
            };
            // stop pause menu on 

            stateMachine.onExit[TrainingStep.HEAD] = (step) =>
            {
#if RUDDER
                RudderPedals.PresenceDetector.Instance.canPause = false;
#endif
            };

            stateMachine.onEnter[TrainingStep.LEFT_ARM] = (step) =>
            {
#if SENSEGLOVE
                audioManager.ScheduleAudioClip(senseGloveAudio.leftArm, queue: true);
                //audioManager.ScheduleAudioClip(senseGloveAudio.leftBall, queue: true);
                PublishNotification("Move your left arm and try to touch the blue ball", senseGloveAudio.leftArm.length +2);
#else
                audioManager.ScheduleAudioClip(controllerAudio.leftArm, queue: false);
                PublishNotification("Press and hold the index trigger and try moving your left arm");
#endif

                audioManager.ScheduleAudioClip(controllerAudio.leftBall, queue: true,
                    onStart: () => handCollectables.Find("HandCollectableLeft").gameObject.SetActive(true));

            };
            stateMachine.onExit[TrainingStep.LEFT_ARM] = (step) =>
            {
                handCollectables.Find("HandCollectableLeft").gameObject.SetActive(false);
            };

            stateMachine.onEnter[TrainingStep.LEFT_HAND] = (step) =>
            {
#if SENSEGLOVE
                PublishNotification("Move your left hand into the blue box", senseGloveAudio.leftHandStart.length + 2);
                audioManager.ScheduleAudioClip(senseGloveAudio.leftHandStart);
                leftCalibrator.OnDone(s => Next(), once: true);
#else
                audioManager.ScheduleAudioClip(controllerAudio.leftHand, queue: true, delay: 0);
                PublishNotification("Press the grip button on the side to close the hand.");
#endif
            };
            stateMachine.onExit[TrainingStep.LEFT_HAND] = (step) =>
            {
#if SENSEGLOVE
                // force stop the calibration, if not done so already
                leftCalibrator.PauseCalibration();
#endif
            };

            stateMachine.onEnter[TrainingStep.RIGHT_ARM] = (step) =>
            {
#if SENSEGLOVE
                audioManager.ScheduleAudioClip(senseGloveAudio.rightArm);
                //audioManager.ScheduleAudioClip(senseGloveAudio.rightBall, queue: true);
                PublishNotification("Move your right arm and try to touch the blue ball");
#else
                audioManager.ScheduleAudioClip(controllerAudio.rightArm, queue: false);
                PublishNotification("Press and hold the index trigger and try moving your right arm");
#endif

                audioManager.ScheduleAudioClip(controllerAudio.rightBall, queue: true,
                    onStart: () => handCollectables.Find("HandCollectableRight").gameObject.SetActive(true));

            };
            stateMachine.onExit[TrainingStep.RIGHT_ARM] = (step) =>
            {
                handCollectables.Find("HandCollectableRight").gameObject.SetActive(false);
            };

            stateMachine.onEnter[TrainingStep.RIGHT_HAND] = (step) =>
            {
#if SENSEGLOVE
                PublishNotification("Move your right hand into the blue box");
                audioManager.ScheduleAudioClip(senseGloveAudio.rightHandStart);
                rightCalibrator.OnDone(s => Next(), once: true);
#else
                audioManager.ScheduleAudioClip(controllerAudio.rightHand, queue: true, delay: 0);
                PublishNotification("Press the grip button to close the hand.");
#endif
            };

#if SENSEGLOVE
            stateMachine.onExit[TrainingStep.RIGHT_HAND] = (step) =>
            {

                rightCalibrator.PauseCalibration();

        };


            stateMachine.onEnter[TrainingStep.ARM_LENGTH] = (step) =>
            {
                //Next();
                armLengthCalibration.OnDone(state => Next(), once: true);
                armLengthCalibration.StartCalibration();
            };
            stateMachine.onExit[TrainingStep.ARM_LENGTH] = (step) =>
            {
                
                armLengthCalibration.StopCalibration();
            };
#endif

            stateMachine.onEnter[TrainingStep.WHEELCHAIR] = (step) =>
            {
                wheelChairTraining.OnDone((s) => Next(), once: true);
               
                wheelChairTraining.StartTraining();
            };
            stateMachine.onExit[TrainingStep.WHEELCHAIR] = (step) =>
            {
                wheelChairTraining.StopTraining();
            };

#if RUDDER
            stateMachine.onEnter[TrainingStep.PAUSE_MENU] = (step) =>
            {
                pauseMenuTraining.OnDone((s) => Next(), once: true);
                pauseMenuTraining.StartTraining();
            };
            stateMachine.onExit[TrainingStep.PAUSE_MENU] = (step) =>
            {
                pauseMenuTraining.StopTraining();
            };

            stateMachine.onEnter[TrainingStep.DONE] = (step) =>
            {
                //audioManager.ScheduleAudioClip(miscAudio.ready);
                RudderPedals.PresenceDetector.Instance.canPause = true;
            };
#else
            stateMachine.onEnter[TrainingStep.PAUSE_MENU] = (step) =>
            {
                PraiseUser();
                audioManager.ScheduleAudioClip(miscAudio.enterButton, queue: true);
            };
#endif
#endregion
        }

        /// <summary>
        /// Shows a message on the notification widget
        /// </summary>
        /// <param name="message">Text to display</param>
        /// <param name="duration">time in seconds to display for</param>
        /// <returns>if the given message was published, i.e. not already existing</returns>
        public static bool PublishNotification(string message, float duration = 4f)
        {
            byte[] color = new byte[] { 0x17, 0x17, 0x17, 0xff };
            ToastrWidget widget = (ToastrWidget)Manager.Instance.FindWidgetWithID(10);
            RosJsonMessage msg = RosJsonMessage.CreateToastrMessage(10, message, duration, color);

            bool isOld = false;
            foreach (var template in widget.toastrActiveQueue)
            {
                if (template.toastrMessage == message && template.toastrDuration == duration)
                {
                    isOld = true;
                    break;
                }
            }
            if (!isOld)
            {
                widget.ProcessRosMessage(msg);
            }
            // published?
            return !isOld;
        }

        public void PraiseUser()
        {
            Debug.Log("Praising user");
            audioManager.ScheduleAudioClip(praisePhrases[UnityEngine.Random.Range(0, praisePhrases.Count)]);
        }

        public void CorrectUser(string correctButton)
        {
            AudioClip audio;
            switch (correctButton)
            {
                case "trigger":
                    audio = miscAudio.wrongTrigger;
                    break;
                case "grip":
                    audio = miscAudio.wrongGrip;
                    break;
                default:
                    audio = miscAudio.wrongButton;
                    break;
            }
           
            if (lastCorrectedAtStep != currentState && (currentState == TrainingStep.LEFT_ARM || currentState == TrainingStep.RIGHT_ARM))
            {
                Debug.Log("Correcting User");
                audioManager.ScheduleAudioClip(miscAudio.wrongTrigger);
                lastCorrectedAtStep = currentState;
            }
        }


        void Update()
        {
            //if (startTraining && !audioManager.IsAudioPlaying() && currentState == TrainingStep.IDLE && !waitStarted)
            //{
            //    waitStarted = true;
            //    StartCoroutine(StartTrainingAfter(3f));
            //}
            //if (currentState == TrainingStep.DONE && !audioManager.IsAudioPlaying())
            //    StateManager.Instance.GoToState(StateManager.States.HUD);
            //if (currentStep == TrainingStep.HEAD && !isAudioPlaying())
            //    waitingForNod = true;

            // allows to continue to the next step when pressing 'n'
            if (Input.GetKeyDown(KeyCode.N))
            {
                StopAllCoroutines();
                //audioManager.ClearQueue();
                Next();
            }
        }

    }


}
