using UnityEngine;

public class AnimatorParameterController : StateMachineBehaviour
{
    [Header("Triggers")]
    public string[] setTriggersOnEnter;
    public string[] resetTriggersOnEnter;
    public string[] setTriggersOnExit;
    public string[] resetTriggersOnExit;

    [Header("Booleans")]
    public string[] setBoolsTrueOnEnter;
    public string[] setBoolsFalseOnEnter;
    public string[] setBoolsTrueOnExit;
    public string[] setBoolsFalseOnExit;

    [Header("Floats")]
    public FloatParameter[] setFloatsOnEnter;
    public FloatParameter[] setFloatsOnExit;

    [System.Serializable]
    public struct FloatParameter
    {
        public string parameterName;
        public float value;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ApplyParameters(
            animator,
            setTriggersOnEnter,
            resetTriggersOnEnter,
            setBoolsTrueOnEnter,
            setBoolsFalseOnEnter,
            setFloatsOnEnter);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ApplyParameters(
            animator,
            setTriggersOnExit,
            resetTriggersOnExit,
            setBoolsTrueOnExit,
            setBoolsFalseOnExit,
            setFloatsOnExit);
    }

    private static void ApplyParameters(
        Animator animator,
        string[] setTriggers,
        string[] resetTriggers,
        string[] setBoolsTrue,
        string[] setBoolsFalse,
        FloatParameter[] setFloats)
    {
        if (animator == null)
        {
            return;
        }

        if (setTriggers != null)
        {
            foreach (var trigger in setTriggers)
            {
                if (!string.IsNullOrWhiteSpace(trigger))
                {
                    animator.SetTrigger(trigger);
                }
            }
        }

        if (resetTriggers != null)
        {
            foreach (var trigger in resetTriggers)
            {
                if (!string.IsNullOrWhiteSpace(trigger))
                {
                    animator.ResetTrigger(trigger);
                }
            }
        }

        if (setBoolsTrue != null)
        {
            foreach (var boolName in setBoolsTrue)
            {
                if (!string.IsNullOrWhiteSpace(boolName))
                {
                    animator.SetBool(boolName, true);
                }
            }
        }

        if (setBoolsFalse != null)
        {
            foreach (var boolName in setBoolsFalse)
            {
                if (!string.IsNullOrWhiteSpace(boolName))
                {
                    animator.SetBool(boolName, false);
                }
            }
        }

        if (setFloats != null)
        {
            foreach (var floatParam in setFloats)
            {
                if (!string.IsNullOrWhiteSpace(floatParam.parameterName))
                {
                    animator.SetFloat(floatParam.parameterName, floatParam.value);
                }
            }
        }
    }
}
