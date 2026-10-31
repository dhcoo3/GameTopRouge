using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

public class UGFUICreateConfig : ScriptableObject
{
    public int ButtonOnClickId = 0;
    public Selectable.Transition ButtonTransition = Selectable.Transition.None;
    public AnimatorController ButtonAnimatorController;
    public Sprite ButtonDefaultImage2;
    public Font DefaultFont;

    public Texture2D InputFieldDefaultBgImage;

    public Texture2D SliderDefaultBgImage;
    public Texture2D SliderDefaultBgImage2;
}