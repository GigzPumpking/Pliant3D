using UnityEngine;
public class SceneTransitionButton : ButtonScript
{
    [Tooltip("The scene to load. Leave empty to use the existing NextScene.TargetScene.")]
    [SerializeField] private string targetScene;

    public override void OnPress()
    {
        if (!string.IsNullOrEmpty(targetScene))
        {
            NextScene.TargetScene = targetScene;
        }

        // Kick off the fade-in animation; NextScene.OnStateExit will
        // load the target scene once the animation finishes.
        UIManager.Instance.FadeIn();
    }

    public override void OnRelease()
    {
        // Nothing needed on release.
    }
}
