using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public class HandInteraction : MonoBehaviour
{
    public ARPianoManager pianoManager;

    XRHandSubsystem handSubsystem;
    readonly Dictionary<int, float> keyCooldowns = new();

    void OnEnable()
    {
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
            handSubsystem = subsystems[0];
    }

    void Update()
    {
        if (handSubsystem == null) return;

        CheckHand(handSubsystem.leftHand);
        CheckHand(handSubsystem.rightHand);

        // Tick down cooldowns
        var keys = new List<int>(keyCooldowns.Keys);
        foreach (var k in keys)
        {
            keyCooldowns[k] -= Time.deltaTime;
            if (keyCooldowns[k] <= 0)
                keyCooldowns.Remove(k);
        }
    }

    void CheckHand(XRHand hand)
    {
        if (!hand.isTracked) return;

        // Check index and middle fingertips
        TryPressWithJoint(hand, XRHandJointID.IndexTip);
        TryPressWithJoint(hand, XRHandJointID.MiddleTip);
        TryPressWithJoint(hand, XRHandJointID.RingTip);
    }

    void TryPressWithJoint(XRHand hand, XRHandJointID jointId)
    {
        var joint = hand.GetJoint(jointId);
        if (!joint.TryGetPose(out Pose pose)) return;

        var pianoKeys = pianoManager.GetAllKeys();
        foreach (var key in pianoKeys)
        {
            int id = key.noteIndex;
            if (keyCooldowns.ContainsKey(id)) continue;

            float dist = Vector3.Distance(pose.position, key.transform.position);
            if (dist < 0.04f)
            {
                key.Press();
                keyCooldowns[id] = 0.3f;
            }
        }
    }
}
