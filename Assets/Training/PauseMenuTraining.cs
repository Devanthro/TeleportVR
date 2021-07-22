using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Training
{
    public class PauseMenuTraining : Automaton<PauseMenuTraining.State>
    {

        public enum State
        {
            INIT,
            START,
            PAUSE,
            UNPAUSE,
            TELEPORT,
            DONE
        }
        public AudioClips.PauseMenu pauseMenuAudio;


        private Callbacks<State> onDoneCallbacks;

        // Start is called before the first frame update
        void Start()
        {
            // neutral state, triggering no callbacks
            currentState = State.INIT;
            onDoneCallbacks = new Callbacks<State>();

            stateMachine.onEnter[State.START] = (state) =>
            {
                TutorialSteps.Instance.audioManager.ScheduleAudioClip(pauseMenuAudio.start, queue: true);
                RudderPedals.PresenceDetector.Instance.canPause = true;
                RudderPedals.PresenceDetector.Instance.pauseAudio = false;
                RudderPedals.PresenceDetector.Instance.OnPause((s) => Next(), once: true);
                PauseMenu.Instance.enabled = false;
            };

            stateMachine.onEnter[State.PAUSE] = (state) =>
            {
                TutorialSteps.Instance.audioManager.ScheduleAudioClip(pauseMenuAudio.paused);
                Next();
            };

            stateMachine.onEnter[State.UNPAUSE] = (state) =>
            {
                TutorialSteps.Instance.audioManager.ScheduleAudioClip(pauseMenuAudio.unpause, queue: true);
                RudderPedals.PresenceDetector.Instance.OnUnpause((s) => Next(), once: true);
            };

            stateMachine.onEnter[State.TELEPORT] = (state) =>
            {
                TutorialSteps.Instance.audioManager.ScheduleAudioClip(pauseMenuAudio.teleport);
                RudderPedals.PresenceDetector.Instance.OnPause((s) => Next(), once: true);
            };

            stateMachine.onEnter[State.DONE] = (state) =>
            {
                RudderPedals.PresenceDetector.Instance.canPause = true;
                RudderPedals.PresenceDetector.Instance.pauseAudio = true;
                PauseMenu.Instance.enabled = true;
                onDoneCallbacks.Call(State.DONE);
            };
        }

        public void StartTraining() => currentState = State.START;

        public void StopTraining() => currentState = State.DONE;

        public void OnDone(System.Action<State> callback, bool once = false) => onDoneCallbacks.Add(callback, once);
    }
}
