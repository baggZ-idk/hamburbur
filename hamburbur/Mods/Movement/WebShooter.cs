using System.Collections.Generic;
using System.Linq;
using GorillaLocomotion;
using hamburbur.GUI;
using hamburbur.Managers;
using hamburbur.Mod_Backend;
using hamburbur.Tools;
using HarmonyLib;
using UnityEngine;

namespace hamburbur.Mods.Movement;

[hamburburmod(                "Web Shooters",       "Gives you Spider Man style web shooters", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class WebShooter : hamburburmod
{
    private const float MaxWebDistance  = 60f;
    private const float WebTravelSpeed  = 100f;
    private const float MinimumShotTime = 0.04f;
    private const float MaximumShotTime = 0.4f;
    private const float MissVisibleTime = 0.08f;

    private const float MinimumRopeLength          = 0.75f;
    private const float InitialRopeTension         = 0.1f;
    private const float RopeSlack                  = 0.035f;
    private const float RopeCorrectionSpeed        = 16f;
    private const float MaximumRopeCorrectionSpeed = 10f;

    private const float PullDeadZone       = 0.12f;
    private const float MaximumPullSpeed   = 4f;
    private const float PullReelMultiplier = 0.6f;
    private const float PullAcceleration   = 10f;

    private const float SwingPumpStrength       = 3.5f;
    private const float SwingMaintainStrength   = 0.8f;
    private const float MaximumAssistedVelocity = 18f;

    private const float FlipRotationSpeed  = 150f;
    private const float RollRotationSpeed  = 90f;
    private const float UprightReturnSpeed = 5f;
    private const float MinimumFlipSpeed   = 1f;

    private const float ReelDeadZone = 0.2f;
    private const float ReelInSpeed  = 1.8f;
    private const float ReelOutSpeed = 2.2f;

    private const float TetherColliderRadius      = 0.17f;
    private const float TetherPullSpring          = 7f;
    private const float TetherPullDamping         = 2.5f;
    private const float MaximumTetherAcceleration = 18f;
    private const float MinimumTetherStopDistance = 0.9f;
    private const float MaximumTetherStopDistance = 1.8f;

    private const int WebSegments          = 20;
    private const int ConstraintIterations = 2;

    private static AudioClip[] shootSounds;

    internal static bool Active;

    private readonly List<WebTether> tethers = new();

    private WebState leftWeb;
    private WebState rightWeb;

    protected override void Start()
    {
        AssetBundle bundle = Plugin.Instance.HamburburBundle;

        shootSounds = bundle.GetAllAssetNames()
                            .Where(assetName =>
                                   {
                                       string fileName = assetName.Split('/').Last().Split('.')[0];

                                       return fileName.StartsWith("web") &&
                                              int.TryParse(fileName[3..], out int _);
                                   })
                            .OrderBy(assetName =>
                                     {
                                         string fileName = assetName.Split('/').Last().Split('.')[0];

                                         return int.Parse(fileName[3..]);
                                     })
                            .Select(bundle.LoadAsset<AudioClip>)
                            .Where(clip => clip != null)
                            .ToArray();
    }

    protected override void OnEnable()
    {
        Active = true;

        TryCreateWebShooters();
    }

    protected override void FixedUpdate()
    {
        if (!TryCreateWebShooters())
            return;

        Rigidbody body = GorillaTagger.Instance.rigidbody;

        UpdateHandMotion(leftWeb);
        UpdateHandMotion(rightWeb);

        UpdateReel(leftWeb,  InputManager.Instance.LeftJoystick.Axis.y);
        UpdateReel(rightWeb, InputManager.Instance.RightJoystick.Axis.y);

        UpdatePull(leftWeb,  body);
        UpdatePull(rightWeb, body);

        for (int i = 0; i < ConstraintIterations; i++)
        {
            ApplyRopeConstraint(leftWeb,  body);
            ApplyRopeConstraint(rightWeb, body);
        }

        ApplySwingAssist(body);
        UpdateTethers();
    }

    protected override void LateUpdate()
    {
        if (!TryCreateWebShooters())
            return;

        UpdateInput(
                leftWeb,
                InputManager.Instance.LeftGrip.IsPressed,
                InputManager.Instance.LeftTrigger.IsPressed);

        UpdateInput(
                rightWeb,
                InputManager.Instance.RightGrip.IsPressed,
                InputManager.Instance.RightTrigger.IsPressed);

        UpdatePendingTetherVisual(leftWeb);
        UpdatePendingTetherVisual(rightWeb);

        UpdateWebVisual(leftWeb);
        UpdateWebVisual(rightWeb);

        UpdateTetherVisuals();
        UpdateSwingRotation();
    }

    protected override void OnDisable()
    {
        Active = false;

        CancelPendingTether(leftWeb);
        CancelPendingTether(rightWeb);

        ReleaseWeb(leftWeb);
        ReleaseWeb(rightWeb);

        DestroyTethers();
        DestroyWebState(leftWeb);
        DestroyWebState(rightWeb);

        leftWeb  = null;
        rightWeb = null;

        if (!SpiderWalk.Active)
            MakeUpright(true);
    }

    private bool TryCreateWebShooters()
    {
        if (leftWeb != null && rightWeb != null)
            return true;

        if (VRRig.LocalRig == null)
            return false;

        Transform leftController  = Tools.Utils.RealLeftController;
        Transform rightController = Tools.Utils.RealRightController;

        Transform leftRigHand  = VRRig.LocalRig.leftHand.rigTarget;
        Transform rightRigHand = VRRig.LocalRig.rightHand.rigTarget;

        if (leftController  == null ||
            rightController == null ||
            leftRigHand     == null ||
            rightRigHand    == null)
        {
            return false;
        }

        leftWeb = CreateWebState(
                leftController,
                leftRigHand,
                true);

        rightWeb = CreateWebState(
                rightController,
                rightRigHand,
                false);

        return true;
    }

    private WebState CreateWebState(Transform hand, Transform rigHand, bool isLeft)
    {
        Transform turnParent = GTPlayer.Instance.turnParent.transform;

        WebState state = new()
        {
                Hand                  = hand,
                IsLeft                = isLeft,
                LastLocalHandPosition = turnParent.InverseTransformPoint(hand.position),
        };

        GameObject shooter = new(isLeft
                                         ? "Left Web Shooter"
                                         : "Right Web Shooter");

        shooter.transform.SetParent(rigHand, false);

        shooter.transform.localPosition = new Vector3(
                isLeft ? -0.0518f : 0.0518f,
                0.0283f,
                -0.0041f);

        shooter.transform.localEulerAngles = isLeft
                                                     ? new Vector3(280f, 270f, 180f)
                                                     : new Vector3(270f, 270f, 0f);

        shooter.transform.localScale = Vector3.one * 0.7f;

        state.Shooter = shooter.transform;

        CreateShooterPart(
                state.Shooter,
                PrimitiveType.Cube,
                "Base",
                Vector3.zero,
                new Vector3(0.065f, 0.018f, 0.08f),
                Quaternion.identity,
                MenuHandler.Instance.currentMainColour);

        CreateShooterPart(
                state.Shooter,
                PrimitiveType.Cube,
                "Plate",
                new Vector3(0f,     0.014f, 0.002f),
                new Vector3(0.044f, 0.012f, 0.05f),
                Quaternion.identity,
                MenuHandler.Instance.currentSecondaryColour);

        CreateShooterPart(
                state.Shooter,
                PrimitiveType.Cylinder,
                "Nozzle",
                new Vector3(0f,    0.004f, 0.045f),
                new Vector3(0.01f, 0.013f, 0.01f),
                Quaternion.Euler(90f, 0f, 0f),
                MenuHandler.Instance.currentSecondaryColour);

        GameObject muzzle = new("Muzzle");

        muzzle.transform.SetParent(state.Shooter, false);
        muzzle.transform.localPosition = new Vector3(0f, 0.004f, 0.064f);
        muzzle.transform.localRotation = Quaternion.identity;

        state.Muzzle = muzzle.transform;

        AudioSource audioSource = shooter.AddComponent<AudioSource>();

        audioSource.playOnAwake  = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume       = 0.8f;
        audioSource.minDistance  = 0.25f;
        audioSource.maxDistance  = 6f;

        state.AudioSource = audioSource;

        GameObject webObject = new(isLeft
                                           ? "Left Web"
                                           : "Right Web");

        LineRenderer line = webObject.AddComponent<LineRenderer>();

        line.useWorldSpace     = true;
        line.positionCount     = WebSegments;
        line.numCapVertices    = 6;
        line.numCornerVertices = 3;
        line.startWidth        = 0.012f;
        line.endWidth          = 0.006f;
        line.startColor        = new Color(0.96f, 0.98f, 1f, 0.98f);
        line.endColor          = new Color(0.88f, 0.93f, 1f, 0.92f);
        line.material          = new Material(Shaders.SpritesDefaultShader);
        line.enabled           = false;

        state.WebObject = webObject;
        state.Line      = line;

        GameObject pendingObject = new(isLeft
                                               ? "Left Pending Tether"
                                               : "Right Pending Tether");

        LineRenderer pendingLine = pendingObject.AddComponent<LineRenderer>();

        pendingLine.useWorldSpace  = true;
        pendingLine.positionCount  = 2;
        pendingLine.numCapVertices = 6;
        pendingLine.startWidth     = 0.01f;
        pendingLine.endWidth       = 0.01f;
        pendingLine.startColor     = line.startColor;
        pendingLine.endColor       = line.endColor;
        pendingLine.material       = new Material(Shaders.SpritesDefaultShader);
        pendingLine.enabled        = false;

        state.PendingWebObject = pendingObject;
        state.PendingLine      = pendingLine;

        return state;
    }

    private static void CreateShooterPart(
            Transform     parent,
            PrimitiveType primitiveType,
            string        name,
            Vector3       position,
            Vector3       scale,
            Quaternion    rotation,
            Color         colour)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);

        part.name = name;

        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = rotation;
        part.transform.localScale    = scale;

        if (part.TryGetComponent(out Collider collider))
            collider.Obliterate();

        if (!part.TryGetComponent(out Renderer renderer))
            return;

        renderer.material.shader = Shaders.UberShader;
        renderer.material.color  = colour;
    }

    private void UpdateInput(WebState state, bool gripPressed, bool tetherPressed)
    {
        if (!tetherPressed &&
            (state.PendingAnchor != null || state.IsTetherShot))
        {
            CancelPendingTether(state);

            if (state.IsTetherShot)
                ReleaseWeb(state);
        }

        switch (gripPressed)
        {
            case true when !state.WasPressed:
                FireWeb(state, tetherPressed);

                break;

            case false when state.WasPressed:
            {
                if (state.IsTetherShot && tetherPressed && state.HasHit)
                    FinishTetherShot(state);
                else
                    ReleaseWeb(state);

                break;
            }
        }

        state.WasPressed = gripPressed;

        if (!state.IsFired)
            return;

        switch (state.HasHit)
        {
            case true when
                    !state.IsAttached &&
                    Time.time >= state.AttachTime:
                AttachWeb(state);

                break;

            case false when Time.time >= state.HideTime:
                state.IsFired      = false;
                state.Line.enabled = false;

                break;
        }

    }

    private void FireWeb(WebState state, bool tetherShot)
    {
        Vector3 origin    = state.Muzzle.position;
        Vector3 direction = state.Hand.forward;

        state.IsFired         = true;
        state.IsAttached      = false;
        state.IsTetherShot    = tetherShot;
        state.HasHit          = false;
        state.AnchorTransform = null;
        state.AnchorBody      = null;
        state.PullSpeed       = 0f;
        state.WobbleStrength  = 0.07f;
        state.ShotStartedAt   = Time.time;

        if (shootSounds is { Length: > 0, } && state.AudioSource != null)
        {
            AudioClip clip = shootSounds[Random.Range(0, shootSounds.Length)];

            state.AudioSource.PlayOneShot(clip);
        }

        bool didHit = Physics.SphereCast(
                origin,
                0.02f,
                direction,
                out RaycastHit hit,
                MaxWebDistance,
                GTPlayer.Instance.locomotionEnabledLayers,
                QueryTriggerInteraction.Ignore);

        if (didHit)
        {
            state.HasHit       = true;
            state.StaticAnchor = hit.point;

            if (hit.collider != null)
            {
                state.AnchorTransform  = hit.collider.transform;
                state.AnchorLocalPoint = state.AnchorTransform.InverseTransformPoint(hit.point);
                state.AnchorBody       = hit.collider.attachedRigidbody;
            }

            float distance = Vector3.Distance(origin, hit.point);

            state.ShotDuration = Mathf.Clamp(
                    distance / WebTravelSpeed,
                    MinimumShotTime,
                    MaximumShotTime);

            state.AttachTime = Time.time + state.ShotDuration;
        }
        else
        {
            state.MissPoint = origin + direction * MaxWebDistance;

            state.ShotDuration = Mathf.Clamp(
                    MaxWebDistance / WebTravelSpeed,
                    MinimumShotTime,
                    MaximumShotTime);

            state.HideTime = Time.time + state.ShotDuration + MissVisibleTime;
        }

        state.Line.enabled = true;
    }

    private void AttachWeb(WebState state)
    {
        state.IsAttached = true;

        Vector3 anchor = GetAnchorPoint(state);

        float distance = Vector3.Distance(
                state.Hand.position,
                anchor);

        state.RopeLength = Mathf.Max(
                MinimumRopeLength,
                distance - InitialRopeTension);

        Transform turnParent = GTPlayer.Instance.turnParent.transform;

        state.LastLocalHandPosition =
                turnParent.InverseTransformPoint(state.Hand.position);

        state.HandVelocity   = Vector3.zero;
        state.PullSpeed      = 0f;
        state.WobbleStrength = 0.055f;
    }

    private static void ReleaseWeb(WebState state)
    {
        if (state == null)
            return;

        state.IsFired         = false;
        state.HasHit          = false;
        state.IsAttached      = false;
        state.IsTetherShot    = false;
        state.AnchorTransform = null;
        state.AnchorBody      = null;
        state.PullSpeed       = 0f;
        state.WobbleStrength  = 0f;

        if (state.Line != null)
            state.Line.enabled = false;
    }

    private static void UpdateReel(WebState state, float joystickY)
    {
        if (state?.IsAttached != true || state.IsTetherShot)
            return;

        if (Mathf.Abs(joystickY) < ReelDeadZone)
            return;

        float speed = joystickY > 0f
                              ? ReelInSpeed
                              : ReelOutSpeed;

        state.RopeLength -= joystickY * speed * Time.fixedDeltaTime;
        state.RopeLength =  Mathf.Clamp(state.RopeLength, MinimumRopeLength, MaxWebDistance);
    }

    private void FinishTetherShot(WebState state)
    {
        WebAnchor anchor = CaptureAnchor(state);

        if (state.PendingAnchor == null)
        {
            state.PendingAnchor = anchor;

            if (state.PendingLine != null)
                state.PendingLine.enabled = true;
        }
        else
        {
            CreateTether(state.PendingAnchor, anchor);
            state.PendingAnchor = null;

            if (state.PendingLine != null)
                state.PendingLine.enabled = false;
        }

        ReleaseWeb(state);
    }

    private static WebAnchor CaptureAnchor(WebState state) =>
            new()
            {
                    Transform  = state.AnchorTransform,
                    LocalPoint = state.AnchorLocalPoint,
                    WorldPoint = state.StaticAnchor,
                    Body       = state.AnchorBody,
            };

    private static void CancelPendingTether(WebState state)
    {
        if (state == null)
            return;

        state.PendingAnchor = null;

        if (state.PendingLine != null)
            state.PendingLine.enabled = false;
    }

    private static void UpdatePendingTetherVisual(WebState state)
    {
        if (state?.PendingAnchor == null || state.PendingLine == null)
            return;

        state.PendingLine.enabled = true;
        state.PendingLine.SetPosition(0, GetAnchorPoint(state.PendingAnchor));
        state.PendingLine.SetPosition(1, state.Muzzle.position);
    }

    private void CreateTether(WebAnchor first, WebAnchor second)
    {
        Vector3 start = GetAnchorPoint(first);
        Vector3 end   = GetAnchorPoint(second);

        float distance = Vector3.Distance(start, end);

        if (distance < 0.2f)
            return;

        GameObject tetherObject = new("Web Tether");
        tetherObject.layer = LayerMask.NameToLayer("Default");

        Rigidbody tetherBody = tetherObject.AddComponent<Rigidbody>();
        tetherBody.isKinematic            = true;
        tetherBody.useGravity             = false;
        tetherBody.interpolation          = RigidbodyInterpolation.Interpolate;
        tetherBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        CapsuleCollider collider = tetherObject.AddComponent<CapsuleCollider>();
        collider.direction = 2;
        collider.radius    = TetherColliderRadius;
        collider.isTrigger = false;

        LineRenderer line = tetherObject.AddComponent<LineRenderer>();
        line.useWorldSpace  = true;
        line.positionCount  = 2;
        line.numCapVertices = 6;
        line.startWidth     = 0.014f;
        line.endWidth       = 0.014f;
        line.startColor     = new Color(0.96f, 0.98f, 1f, 0.98f);
        line.endColor       = new Color(0.88f, 0.93f, 1f, 0.92f);
        line.material       = new Material(Shaders.SpritesDefaultShader);

        WebTether tether = new()
        {
                First      = first,
                Second     = second,
                GameObject = tetherObject,
                Body       = tetherBody,
                Collider   = collider,
                Line       = line,
                StopDistance = Mathf.Clamp(
                        distance * 0.08f,
                        MinimumTetherStopDistance,
                        MaximumTetherStopDistance),
        };

        tethers.Add(tether);
        SetTetherGeometry(tether, true);
    }

    private void UpdateTethers()
    {
        foreach (WebTether tether in tethers)
        {
            SetTetherGeometry(tether, false);
            ApplyTetherForces(tether);
        }
    }

    private void UpdateTetherVisuals()
    {
        foreach (WebTether tether in tethers)
        {
            if (tether.Line == null)
                continue;

            tether.Line.SetPosition(0, GetAnchorPoint(tether.First));
            tether.Line.SetPosition(1, GetAnchorPoint(tether.Second));
        }
    }

    private static void SetTetherGeometry(WebTether tether, bool immediate)
    {
        Vector3 start = GetAnchorPoint(tether.First);
        Vector3 end   = GetAnchorPoint(tether.Second);
        Vector3 delta = end - start;

        float distance = delta.magnitude;

        if (distance < 0.001f)
            return;

        Vector3 direction = delta / distance;
        Vector3 middle    = Vector3.Lerp(start, end, 0.5f);

        Vector3 up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.98f
                             ? Vector3.forward
                             : Vector3.up;

        Quaternion rotation = Quaternion.LookRotation(direction, up);

        if (immediate)
        {
            tether.GameObject.transform.SetPositionAndRotation(middle, rotation);
        }
        else
        {
            tether.Body.MovePosition(middle);
            tether.Body.MoveRotation(rotation);
        }

        tether.Collider.height = Mathf.Max(
                distance + TetherColliderRadius * 2f,
                TetherColliderRadius * 2f);
    }

    private static void ApplyTetherForces(WebTether tether)
    {
        Rigidbody firstBody  = GetDynamicBody(tether.First.Body);
        Rigidbody secondBody = GetDynamicBody(tether.Second.Body);

        if (firstBody == secondBody)
            return;

        if (firstBody == null && secondBody == null)
            return;

        Vector3 firstPoint  = GetAnchorPoint(tether.First);
        Vector3 secondPoint = GetAnchorPoint(tether.Second);
        Vector3 delta       = secondPoint - firstPoint;

        float distance = delta.magnitude;

        if (distance < 0.001f)
            return;

        Vector3 direction = delta / distance;

        Vector3 firstVelocity = firstBody != null
                                        ? firstBody.GetPointVelocity(firstPoint)
                                        : Vector3.zero;

        Vector3 secondVelocity = secondBody != null
                                         ? secondBody.GetPointVelocity(secondPoint)
                                         : Vector3.zero;

        float distanceSpeed = Vector3.Dot(
                secondVelocity - firstVelocity,
                direction);

        float stretch = distance - tether.StopDistance;

        if (stretch <= 0f)
        {
            if (distanceSpeed >= -0.05f)
                return;

            float brake = Mathf.Min(
                    -distanceSpeed * TetherPullDamping,
                    MaximumTetherAcceleration);

            if (firstBody != null && secondBody != null)
            {
                firstBody.AddForce(-direction * brake * 0.5f, ForceMode.Acceleration);
                secondBody.AddForce(direction * brake * 0.5f, ForceMode.Acceleration);
            }
            else if (firstBody != null)
            {
                firstBody.AddForce(-direction * brake, ForceMode.Acceleration);
            }
            else
            {
                secondBody.AddForce(direction * brake, ForceMode.Acceleration);
            }

            return;
        }

        float acceleration = Mathf.Clamp(
                stretch       * TetherPullSpring +
                distanceSpeed * TetherPullDamping,
                0f,
                MaximumTetherAcceleration);

        if (firstBody != null)
        {
            firstBody.AddForceAtPosition(
                    direction * acceleration,
                    firstPoint,
                    ForceMode.Acceleration);
        }

        if (secondBody != null)
        {
            secondBody.AddForceAtPosition(
                    -direction * acceleration,
                    secondPoint,
                    ForceMode.Acceleration);
        }
    }

    private static void UpdateHandMotion(WebState state)
    {
        if (state == null || state.Hand == null)
            return;

        Transform turnParent = GTPlayer.Instance.turnParent.transform;

        Vector3 localPosition =
                turnParent.InverseTransformPoint(state.Hand.position);

        float deltaTime = Mathf.Max(
                Time.fixedDeltaTime,
                0.001f);

        Vector3 localVelocity =
                (localPosition - state.LastLocalHandPosition) /
                deltaTime;

        state.LastLocalHandPosition = localPosition;

        Vector3 worldVelocity =
                turnParent.TransformVector(localVelocity);

        state.HandVelocity = Vector3.Lerp(
                state.HandVelocity,
                worldVelocity,
                0.65f);
    }

    private static void UpdatePull(WebState state, Rigidbody body)
    {
        if (state?.IsAttached != true || state.IsTetherShot)
            return;

        Vector3 anchor   = GetAnchorPoint(state);
        Vector3 toAnchor = anchor - state.Hand.position;

        float distance = toAnchor.magnitude;

        if (distance < 0.001f)
            return;

        Vector3 ropeDirection = toAnchor / distance;

        float pullSpeed = -Vector3.Dot(
                                  state.HandVelocity,
                                  ropeDirection);

        pullSpeed = Mathf.Clamp(pullSpeed, 0f, MaximumPullSpeed);

        float smoothing = 1f - Mathf.Exp(-18f * Time.fixedDeltaTime);

        state.PullSpeed = Mathf.Lerp(
                state.PullSpeed,
                pullSpeed,
                smoothing);

        float activePull = state.PullSpeed - PullDeadZone;

        if (activePull <= 0f)
            return;

        state.RopeLength = Mathf.Max(
                MinimumRopeLength,
                state.RopeLength -
                activePull * PullReelMultiplier * Time.fixedDeltaTime);

        Rigidbody targetBody = GetDynamicBody(state.AnchorBody);

        if (targetBody != null)
        {
            targetBody.AddForceAtPosition(
                    -ropeDirection * activePull * PullAcceleration,
                    anchor,
                    ForceMode.Acceleration);

            return;
        }

        body.AddForce(
                ropeDirection * activePull * PullAcceleration,
                ForceMode.Acceleration);
    }

    private static void ApplyRopeConstraint(WebState state, Rigidbody body)
    {
        if (state?.IsAttached != true || state.IsTetherShot)
            return;

        Vector3 anchor   = GetAnchorPoint(state);
        Vector3 toAnchor = anchor - state.Hand.position;

        float distance = toAnchor.magnitude;

        if (distance < 0.001f)
            return;

        Vector3 ropeDirection = toAnchor / distance;
        float   stretch       = distance - state.RopeLength;

        if (stretch < -RopeSlack)
            return;

        float     tautness   = Mathf.InverseLerp(-RopeSlack, 0f, stretch);
        Rigidbody targetBody = GetDynamicBody(state.AnchorBody);

        if (targetBody != null)
        {
            Vector3 handVelocity   = body.linearVelocity + state.HandVelocity;
            Vector3 anchorVelocity = targetBody.GetPointVelocity(anchor);

            float separatingSpeed = Vector3.Dot(
                    anchorVelocity - handVelocity,
                    ropeDirection);

            if (separatingSpeed > 0f)
            {
                targetBody.linearVelocity -=
                        ropeDirection * separatingSpeed * tautness;
            }

            if (stretch <= 0f)
                return;

            float correctionSpeed = Mathf.Min(
                    stretch * RopeCorrectionSpeed,
                    MaximumRopeCorrectionSpeed);

            correctionSpeed /= ConstraintIterations;

            targetBody.linearVelocity -=
                    ropeDirection * correctionSpeed;

            return;
        }

        Vector3 endVelocity    = body.linearVelocity + state.HandVelocity;
        float   radialVelocity = Vector3.Dot(endVelocity, ropeDirection);

        if (radialVelocity < 0f)
        {
            body.linearVelocity +=
                    ropeDirection * -radialVelocity * tautness;
        }

        if (stretch <= 0f)
            return;

        float playerCorrectionSpeed = Mathf.Min(
                stretch * RopeCorrectionSpeed,
                MaximumRopeCorrectionSpeed);

        playerCorrectionSpeed /= ConstraintIterations;

        body.linearVelocity +=
                ropeDirection * playerCorrectionSpeed;
    }

    private void ApplySwingAssist(Rigidbody body)
    {
        Vector3 ropeDirection = Vector3.zero;
        Vector3 handForward   = Vector3.zero;

        float pullAmount = 0f;

        int count = 0;

        AddSwingData(
                leftWeb,
                ref ropeDirection,
                ref handForward,
                ref pullAmount,
                ref count);

        AddSwingData(
                rightWeb,
                ref ropeDirection,
                ref handForward,
                ref pullAmount,
                ref count);

        if (count == 0)
            return;

        ropeDirection.Normalize();
        handForward.Normalize();

        Vector3 tangentVelocity =
                Vector3.ProjectOnPlane(
                        body.linearVelocity,
                        ropeDirection);

        if (tangentVelocity.sqrMagnitude  > 0.25f &&
            body.linearVelocity.magnitude < MaximumAssistedVelocity)
        {
            body.AddForce(
                    tangentVelocity.normalized *
                    SwingMaintainStrength,
                    ForceMode.Acceleration);
        }

        pullAmount /= count;

        if (pullAmount <= PullDeadZone)
            return;

        Transform head = GTPlayer.Instance.headCollider.transform;

        Vector3 wantedDirection =
                head.forward * 0.75f +
                handForward  * 0.25f;

        Vector3 swingDirection =
                Vector3.ProjectOnPlane(
                        wantedDirection,
                        ropeDirection);

        if (swingDirection.sqrMagnitude < 0.001f)
            return;

        swingDirection.Normalize();

        float pump =
                Mathf.Clamp(
                        pullAmount - PullDeadZone,
                        0f,
                        MaximumPullSpeed);

        float speedScale =
                Mathf.InverseLerp(
                        MaximumAssistedVelocity,
                        MaximumAssistedVelocity - 5f,
                        body.linearVelocity.magnitude);

        body.AddForce(
                swingDirection    *
                pump              *
                SwingPumpStrength *
                speedScale,
                ForceMode.Acceleration);
    }

    private static void AddSwingData(
            WebState    state,
            ref Vector3 ropeDirection,
            ref Vector3 handForward,
            ref float   pullAmount,
            ref int     count)
    {
        if (!IsSwingWeb(state))
            return;

        Vector3 direction =
                GetAnchorPoint(state) -
                state.Hand.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        ropeDirection += direction.normalized;
        handForward   += state.Hand.forward;
        pullAmount    += state.PullSpeed;

        count++;
    }

    private void UpdateWebVisual(WebState state)
    {
        if (state == null || state.Line == null)
            return;

        if (!state.IsFired)
        {
            state.Line.enabled = false;

            return;
        }

        state.Line.enabled = true;

        Vector3 start = state.Muzzle.position;

        Vector3 target = state.HasHit
                                 ? GetAnchorPoint(state)
                                 : state.MissPoint;

        float progress = Mathf.Clamp01(
                (Time.time - state.ShotStartedAt) /
                Mathf.Max(state.ShotDuration, 0.001f));

        if (state.IsAttached)
            progress = 1f;

        float easedProgress =
                1f -
                Mathf.Pow(
                        1f - progress,
                        3f);

        Vector3 end =
                Vector3.Lerp(
                        start,
                        target,
                        easedProgress);

        Vector3 direction =
                end - start;

        float distance =
                direction.magnitude;

        if (distance < 0.001f)
            return;

        direction /= distance;

        Vector3 side =
                Vector3.Cross(
                        direction,
                        Vector3.up);

        if (side.sqrMagnitude < 0.001f)
            side = Vector3.Cross(direction, Vector3.right);

        side.Normalize();

        Vector3 up =
                Vector3.Cross(
                        direction,
                        side).normalized;

        float ropeDistance =
                state.IsAttached
                        ? Vector3.Distance(
                                state.Hand.position,
                                target)
                        : distance;

        float slack =
                state.IsAttached
                        ? Mathf.Max(
                                0f,
                                state.RopeLength - ropeDistance)
                        : 0f;

        float sag = Mathf.Clamp(
                distance * 0.012f +
                slack    * 0.35f,
                0f,
                0.28f);

        if (!state.IsAttached)
            sag *= progress;

        float speed =
                GorillaTagger.Instance.rigidbody.linearVelocity.magnitude;

        float targetWobble =
                state.IsAttached
                        ? 0.007f + Mathf.Clamp(speed * 0.002f, 0f, 0.03f)
                        : 0.045f;

        state.WobbleStrength = Mathf.Lerp(
                state.WobbleStrength,
                targetWobble,
                Time.deltaTime * 8f);

        Vector3 gravityDirection =
                Physics.gravity.sqrMagnitude > 0.001f
                        ? Physics.gravity.normalized
                        : Vector3.down;

        for (int i = 0; i < WebSegments; i++)
        {
            float t =
                    i /
                    (float)(WebSegments - 1);

            Vector3 point =
                    Vector3.Lerp(
                            start,
                            end,
                            t);

            float envelope =
                    Mathf.Sin(
                            t *
                            Mathf.PI);

            float wave =
                    Mathf.Sin(
                            t         * 17f -
                            Time.time * 13f +
                            (state.IsLeft ? 0f : 1.7f));

            float secondWave =
                    Mathf.Sin(
                            t         * 10f +
                            Time.time * 8f  +
                            (state.IsLeft ? 2f : 0f));

            point +=
                    side                 *
                    wave                 *
                    state.WobbleStrength *
                    envelope;

            point +=
                    up                   *
                    secondWave           *
                    state.WobbleStrength *
                    0.4f                 *
                    envelope;

            point +=
                    gravityDirection *
                    sag              *
                    envelope;

            state.Line.SetPosition(i, point);
        }
    }

    private void UpdateSwingRotation()
    {
        if (SpiderWalk.Active)
            return;

        bool leftAttached  = IsSwingWeb(leftWeb);
        bool rightAttached = IsSwingWeb(rightWeb);

        if (!leftAttached && !rightAttached)
        {
            MakeUpright(false);

            return;
        }

        Rigidbody body = GorillaTagger.Instance.rigidbody;

        if (body.linearVelocity.magnitude < MinimumFlipSpeed)
            return;

        Transform turnParent =
                GTPlayer.Instance.turnParent.transform;

        Transform head =
                GTPlayer.Instance.headCollider.transform;

        Vector3 averageHand = Vector3.zero;

        int count = 0;

        if (leftAttached)
        {
            averageHand += leftWeb.Hand.position;
            count++;
        }

        if (rightAttached)
        {
            averageHand += rightWeb.Hand.position;
            count++;
        }

        if (count == 0)
            return;

        averageHand /= count;

        Vector3 handsFromHead =
                averageHand -
                head.position;

        if (handsFromHead.sqrMagnitude < 0.001f)
            return;

        handsFromHead.Normalize();

        Vector3 localLook =
                turnParent.InverseTransformDirection(
                        head.forward);

        Vector3 localHands =
                turnParent.InverseTransformDirection(
                        handsFromHead);

        float pitchInput = Mathf.Clamp(
                localLook.y  * 0.75f +
                localHands.y * 0.45f,
                -1f,
                1f);

        float rollInput = Mathf.Clamp(
                localHands.x * 0.65f,
                -1f,
                1f);

        float speedScale = Mathf.InverseLerp(
                MinimumFlipSpeed,
                8f,
                body.linearVelocity.magnitude);

        float pitchAngle =
                -pitchInput       *
                FlipRotationSpeed *
                speedScale        *
                Time.deltaTime;

        float rollAngle =
                -rollInput        *
                RollRotationSpeed *
                speedScale        *
                Time.deltaTime;

        Quaternion pitch =
                Quaternion.AngleAxis(
                        pitchAngle,
                        head.right);

        Quaternion roll =
                Quaternion.AngleAxis(
                        rollAngle,
                        head.forward);

        turnParent.rotation =
                roll  *
                pitch *
                turnParent.rotation;
    }

    private static void MakeUpright(bool immediate)
    {
        if (!GTPlayer.hasInstance ||
            GTPlayer.Instance.turnParent == null)
        {
            return;
        }

        Transform turnParent =
                GTPlayer.Instance.turnParent.transform;

        Transform head =
                GTPlayer.Instance.headCollider.transform;

        Vector3 forward =
                Vector3.ProjectOnPlane(
                        turnParent.forward,
                        Vector3.up);

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.ProjectOnPlane(
                    head.forward,
                    Vector3.up);
        }

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        Quaternion upright =
                Quaternion.LookRotation(
                        forward.normalized,
                        Vector3.up);

        if (immediate)
        {
            turnParent.rotation = upright;

            return;
        }

        float amount =
                1f -
                Mathf.Exp(
                        -UprightReturnSpeed *
                        Time.deltaTime);

        turnParent.rotation =
                Quaternion.Slerp(
                        turnParent.rotation,
                        upright,
                        amount);
    }

    private static Vector3 GetAnchorPoint(WebState state)
    {
        if (state.AnchorTransform != null)
        {
            return state.AnchorTransform.TransformPoint(
                    state.AnchorLocalPoint);
        }

        return state.StaticAnchor;
    }

    private static Vector3 GetAnchorPoint(WebAnchor anchor)
    {
        if (anchor.Transform != null)
            return anchor.Transform.TransformPoint(anchor.LocalPoint);

        return anchor.WorldPoint;
    }

    private static Rigidbody GetDynamicBody(Rigidbody body)
    {
        if (body == null || body.isKinematic)
            return null;

        return body;
    }

    private static bool IsSwingWeb(WebState state) =>
            state?.IsAttached == true &&
            !state.IsTetherShot       &&
            GetDynamicBody(state.AnchorBody) == null;

    private void DestroyTethers()
    {
        foreach (WebTether tether in tethers)
        {
            if (tether.Line?.material != null)
                Object.Destroy(tether.Line.material);

            if (tether.GameObject != null)
                tether.GameObject.Obliterate();
        }

        tethers.Clear();
    }

    private static void DestroyWebState(WebState state)
    {
        if (state == null)
            return;

        if (state.Line?.material != null)
            Object.Destroy(state.Line.material);

        if (state.PendingLine?.material != null)
            Object.Destroy(state.PendingLine.material);

        if (state.WebObject != null)
            state.WebObject.Obliterate();

        if (state.PendingWebObject != null)
            state.PendingWebObject.Obliterate();

        if (state.Shooter != null)
            state.Shooter.gameObject.Obliterate();
    }

    private sealed class WebState
    {
        public Rigidbody AnchorBody;
        public Vector3   AnchorLocalPoint;
        public Transform AnchorTransform;

        public float       AttachTime;
        public AudioSource AudioSource;

        public Transform Hand;
        public Vector3   HandVelocity;
        public bool      HasHit;
        public float     HideTime;
        public bool      IsAttached;
        public bool      IsFired;
        public bool      IsLeft;
        public bool      IsTetherShot;
        public Vector3   LastLocalHandPosition;

        public LineRenderer Line;
        public Vector3      MissPoint;
        public Transform    Muzzle;

        public WebAnchor    PendingAnchor;
        public LineRenderer PendingLine;
        public GameObject   PendingWebObject;

        public float     PullSpeed;
        public float     RopeLength;
        public Transform Shooter;
        public float     ShotDuration;
        public float     ShotStartedAt;

        public Vector3 StaticAnchor;
        public bool    WasPressed;

        public GameObject WebObject;
        public float      WobbleStrength;
    }

    private sealed class WebAnchor
    {
        public Rigidbody Body;
        public Vector3   LocalPoint;
        public Transform Transform;
        public Vector3   WorldPoint;
    }

    private sealed class WebTether
    {
        public Rigidbody       Body;
        public CapsuleCollider Collider;
        public WebAnchor       First;

        public GameObject   GameObject;
        public LineRenderer Line;
        public WebAnchor    Second;

        public float StopDistance;
    }
}

[HarmonyPatch(typeof(VRRig), nameof(VRRig.PostTick))]
public static class WebShooterRigPatch
{
    private static void Postfix(VRRig __instance)
    {
        if (!WebShooter.Active       ||
            SpiderWalk.Active        ||
            !__instance.IsLocalRig() ||
            !GTPlayer.hasInstance)
        {
            return;
        }

        Transform turnParent =
                GTPlayer.Instance.turnParent.transform;

        Vector3 up =
                turnParent.up;

        if (Vector3.Dot(up, Vector3.up) > 0.9999f)
            return;

        Quaternion tilt =
                Quaternion.FromToRotation(
                        Vector3.up,
                        up);

        __instance.transform.rotation =
                tilt *
                __instance.transform.rotation;

        __instance.leftHand.MapMine(
                __instance.scaleFactor,
                __instance.playerOffsetTransform);

        __instance.rightHand.MapMine(
                __instance.scaleFactor,
                __instance.playerOffsetTransform);

        __instance.head.MapMine(
                __instance.scaleFactor,
                __instance.playerOffsetTransform);

        __instance.head.rigTarget.rotation =
                GTPlayer.Instance.headCollider.transform.rotation;
    }
}