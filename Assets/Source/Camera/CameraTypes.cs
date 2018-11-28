/////////////////////////////////////////////////////////////////
// CameraTypes.cs
//
// Author: Keith Wolf
/////////////////////////////////////////////////////////////////
using UnityEngine;

/// <summary>
/// 
/// </summary>
public enum ViewTargetBlendType
{
    /// <summary>
    /// Camera does a simple linear interpolation.
    /// </summary>
    Linear,

    /// <summary>
    /// Camera has a slight ease-in and ease-out, but amount of ease cannot be tweaked.
    /// </summary>
    Cubic,

    /// <summary>
    /// Camera immediately accelerates, but smoothly decelerates into the target.
    /// </summary>
    EaseIn,

    /// <summary>
    /// Camera smoothly accelerates, but does not decelerate into the target.
    /// </summary>
    EaseOut,

    /// <summary>
    /// Camera smoothly accelerates and decelerates.
    /// </summary>
    EaseInOut,
};

/// <summary>
/// A set of parameters to describe how to transition between view targets.
/// </summary>
public class FBViewTargetTransitionParams
{
    /// <summary>
    /// The total duration of blend to pending view target. 0 means no blending.
    /// </summary>
    public float totalBlendTime;

    /// <summary>
    /// The type of blending to apply to this transition.
    /// </summary>
    public ViewTargetBlendType blendType;

    /// <summary>
    /// An exponent is used to control the shape of the blend curve.
    /// </summary>
    public float blendExponent;

    /// <summary>
    /// This is useful if you want to teleport the view target, but want to keep the camera motion smooth.
    /// </summary>
    public bool lockOutgoing;

    /// <summary>
    /// Creates a new instance with default transition values.
    /// </summary>
    public FBViewTargetTransitionParams( )
    {
        blendExponent = 0.2f;
        lockOutgoing = false;
        blendType = ViewTargetBlendType.Cubic;
    }
};

/// <summary>
/// A view target is responsible for providing the camera with an ideal point of view.
/// </summary>
public struct ViewTarget
{
    /// <summary>
    /// The view target used to compute a point of view.
    /// </summary>
    public CameraActor actor;

    /// <summary>
    /// Point Of View.
    /// </summary>
    public PointOfView pointOfView;
};

/// <summary>
/// Defines a point of view.
/// </summary>
public struct PointOfView
{
    /// <summary>
    /// Position.
    /// </summary>
    public Vector3 position;

    /// <summary>
    /// Rotation.
    /// </summary>
    public Quaternion rotation;

    /// <summary>
    /// Field of view angle.
    /// </summary>
    public float fieldOfView;
};

/// <summary>
/// Cache of the cameras point of view at a specific time.
/// </summary>
public struct CameraCache
{
    /// <summary>
    /// Cached time stamp.
    /// </summary>
    public float timeStamp;

    /// <summary>
    /// Cached point of view.
    /// </summary>
    public PointOfView pointOfView;
};
