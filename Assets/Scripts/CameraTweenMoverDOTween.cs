
using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public class CameraTweenMoverDOTween : MonoBehaviour
{
    [Header("Tween Settings")]
    public float defaultDuration = 1.2f;
    public Ease ease = Ease.InOutSine;
    public bool tweenRotation = true;

    [Header("Controller Integration")]
    public bool temporarilyDisableController = true;

    //Change the type here to your actual controller class name if different
    public SceneCameraController cameraController;

    [Header("Return Behavior")]
    public bool storePoseBeforeMove = true;

    [Header("Advanced")]
    public bool updateIndependentOfTimeScale = false;

    private Sequence seq;
    private Vector3 prevPos;
    private Quaternion prevRot;
    private bool hasPrev;

    public void MoveTo(Transform target)
    {
        if (target == null) return;
        MoveTo(target.position, target.rotation, defaultDuration);
    }

    public void MoveTo(Vector3 worldPos, Quaternion worldRot, float duration)
    {
        duration = Mathf.Max(0.01f, duration);
        KillTween();

        if (storePoseBeforeMove)
        {
            prevPos = transform.position;
            prevRot = transform.rotation;
            hasPrev = true;
        }

        SetControllerEnabled(false);

        seq = DOTween.Sequence();
        seq.Join(transform.DOMove(worldPos, duration).SetEase(ease));

        if (tweenRotation)
            seq.Join(transform.DORotateQuaternion(worldRot, duration).SetEase(ease));

        if (updateIndependentOfTimeScale) seq.SetUpdate(true);

        seq.OnComplete(() =>
        {
            //Sync controller to the new pose so it won't drag us back
            if (cameraController != null)
            {
                cameraController.SyncToCurrentTransformPose(recenterPivotInFront: true, fallbackDistance: 5f);
            }

            SetControllerEnabled(true);
            KillTween();
        })
        .OnKill(() =>
        {
            //Ensure controller is not left disabled if tween is interrupted
            SetControllerEnabled(true);
        });
    }

    public void ReturnToPrevious()
    {
        if (!hasPrev) return;
        MoveTo(prevPos, prevRot, defaultDuration);
    }

    public void CancelTween()
    {
        KillTween();
        SetControllerEnabled(true);
    }

    private void KillTween()
    {
        if (seq != null && seq.IsActive()) seq.Kill();
        seq = null;
    }

    private void SetControllerEnabled(bool enabled)
    {
        if (!temporarilyDisableController) return;
        if (cameraController != null) cameraController.enabled = enabled;
    }
}
