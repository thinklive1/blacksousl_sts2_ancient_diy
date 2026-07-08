using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System.Reflection;

namespace BlackSouls.Scripts;

/// <summary>Controls Cheshire Cat companion visuals.</summary>
public partial class CheshireCatPetVisuals : NCreatureVisuals
{
    private const string CatAnimation = "animation";
    private const float SmileFadedAlpha = 0.35f;
    private const double SmileFadeDuration = 0.8;

    private Node2D? _smileVisuals;
    private Node2D? _biteVisuals;
    private MegaSprite? _smileSprite;
    private MegaSprite? _biteSprite;

    public override void _Ready()
    {
        base._Ready();

        _smileVisuals = GetNodeOrNull<Node2D>("%SmileVisuals");
        _biteVisuals = GetNodeOrNull<Node2D>("%BiteVisuals");
        _smileSprite = CreateSprite(_smileVisuals);
        _biteSprite = CreateSprite(_biteVisuals);

        ShowSmileIdle();
    }

    public void ShowSmileIdle()
    {
        if (_biteVisuals != null)
        {
            _biteVisuals.Visible = false;
        }

        if (_smileVisuals == null)
        {
            return;
        }

        _smileVisuals.Visible = true;
        _smileVisuals.Modulate = Colors.White;
        PlaySpineAnimation(_smileSprite, CatAnimation, loop: true);
    }

    public void PlaySmile()
    {
        if (_biteVisuals != null)
        {
            _biteVisuals.Visible = false;
        }

        if (_smileVisuals == null)
        {
            return;
        }

        _smileVisuals.Visible = true;
        _smileVisuals.Modulate = Colors.White;
        PlaySpineAnimation(_smileSprite, CatAnimation, loop: false);
        QueueSpineAnimation(_smileSprite, CatAnimation, loop: true);
        _smileVisuals.CreateTween().TweenProperty(_smileVisuals, "modulate:a", SmileFadedAlpha, SmileFadeDuration);
    }

    public void PlayBite()
    {
        if (_smileVisuals != null)
        {
            _smileVisuals.Visible = false;
        }

        if (_biteVisuals == null)
        {
            return;
        }

        _biteVisuals.Visible = true;
        _biteVisuals.Modulate = Colors.White;
        PlaySpineAnimation(_biteSprite, CatAnimation, loop: false);
    }

    private static MegaSprite? CreateSprite(Node2D? node)
    {
        if (node == null || node.GetClass() != "SpineSprite")
        {
            return null;
        }

        try
        {
            MegaSprite sprite = new(node);
            return sprite.GetSkeleton()?.GetData() == null ? null : sprite;
        }
        catch
        {
            return null;
        }
    }

    private static void PlaySpineAnimation(MegaSprite? sprite, string animation, bool loop)
    {
        InvokeAnimationState(sprite, "SetAnimation", animation, loop, delay: null);
    }

    private static void QueueSpineAnimation(MegaSprite? sprite, string animation, bool loop)
    {
        InvokeAnimationState(sprite, "AddAnimation", animation, loop, delay: 0f);
    }

    private static void InvokeAnimationState(MegaSprite? sprite, string methodName, string animation, bool loop, float? delay)
    {
        object? animationState;
        try
        {
            animationState = sprite?.GetAnimationState();
        }
        catch
        {
            return;
        }

        if (animationState == null)
        {
            return;
        }

        MethodInfo? method = FindAnimationMethod(animationState.GetType(), methodName, delay.HasValue);
        if (method == null)
        {
            return;
        }

        object?[] args = BuildAnimationArguments(method, animation, loop, delay);
        try
        {
            method.Invoke(animationState, args);
        }
        catch
        {
            // Visual animation is non-critical; gameplay should continue if the spine API changes.
        }
    }

    private static MethodInfo? FindAnimationMethod(Type animationStateType, string methodName, bool needsDelay)
    {
        foreach (MethodInfo method in animationStateType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            if (method.Name != methodName)
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            bool hasAnimation = parameters.Any(parameter => parameter.ParameterType == typeof(string));
            bool hasLoop = parameters.Any(parameter => parameter.ParameterType == typeof(bool));
            bool hasDelay = parameters.Any(parameter => parameter.ParameterType == typeof(float));
            if (hasAnimation && hasLoop && (!needsDelay || hasDelay))
            {
                return method;
            }
        }

        return null;
    }

    private static object?[] BuildAnimationArguments(MethodInfo method, string animation, bool loop, float? delay)
    {
        ParameterInfo[] parameters = method.GetParameters();
        object?[] args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            Type parameterType = parameters[i].ParameterType;
            if (parameterType == typeof(string))
            {
                args[i] = animation;
            }
            else if (parameterType == typeof(bool))
            {
                args[i] = loop;
            }
            else if (parameterType == typeof(float))
            {
                args[i] = delay ?? 0f;
            }
            else if (parameterType == typeof(int))
            {
                args[i] = 0;
            }
            else
            {
                args[i] = parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
            }
        }

        return args;
    }
}
