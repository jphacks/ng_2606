using DeskStage.Gameplay;
using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>
    /// 立体のキャラ（CharacterBody3D）の手足を、キャラの動き（CharacterMotor）に合わせて動かす（2026-11-10）。角度は LimbPose が決める。
    /// 動きの速さは、毎フレームの位置の変わり方から測る（C のキャラの中身には手を入れない）。見た目だけ。
    /// 足は、進んだ距離の分だけ運ぶ（歩幅は LimbPose.CycleLength。足がすべって見えない）。
    /// なめらかにするのは、振りの大きさ（歩き出し・止まる）と、地上と空中の姿勢の切り替え（跳ぶ・着地）。歩くと、体を少し前へ傾ける。
    /// </summary>
    public class CharacterLimbs : MonoBehaviour
    {

        /// <summary>目標の姿勢へ近づく速さ（1 秒あたり。大きいほど、すばやく追いつく）。</summary>
        const float Follow = 12f;

        /// <summary>最高の速さで歩くときに、体を前へ傾ける角度（度）。</summary>
        const float WalkLean = 8f;

        CharacterMotor motor;
        Transform[] joints;
        Vector3[] rest;
        float height;
        float phase;
        Vector3 lastPosition;
        float verticalSpeed;
        float horizontalSpeed;
        float stepDistance; // このフレームに、横へ進んだ距離（m）
        float swingScale; // 歩きの振りの大きさ（0〜1）
        float air;        // 空中の姿勢の混ぜ具合（0：地上、1：空中）
        LimbPose.Pose airPose;
        float lean;

        public void Set(CharacterMotor target, Transform[] partJoints, float bodyHeight)
        {
            motor = target;
            joints = partJoints;
            height = bodyHeight;
            rest = new Vector3[joints.Length];
            for (var i = 0; i < joints.Length; i++) rest[i] = joints[i].localPosition;
            lastPosition = motor.transform.position;
        }

        void LateUpdate()
        {
            if (motor == null || joints == null) return;
            var dt = Time.deltaTime;
            var position = motor.transform.position;
            if (dt > 0f)
            {
                var delta = position - lastPosition;
                // 瞬間移動（ワープ、スタートへ戻る）したときは、速さに入れない
                stepDistance = 0f;
                if (delta.magnitude < 0.05f)
                {
                    stepDistance = new Vector2(delta.x, delta.z).magnitude;
                    horizontalSpeed = Mathf.Lerp(horizontalSpeed, new Vector2(delta.x, delta.z).magnitude / dt, 0.3f);
                    verticalSpeed = Mathf.Lerp(verticalSpeed, delta.y / dt, 0.5f);
                }
            }
            lastPosition = position;

            var walk = motor.settings != null && motor.settings.walkSpeed > 0f ? motor.settings.walkSpeed : 0.15f;
            var speed01 = Mathf.Clamp01(horizontalSpeed / walk);
            // 足の運び：進んだ距離の分だけ、位相を進める（1 往復で CycleLength 進む）。地面にいるときだけ
            if (motor.IsGrounded)
            {
                phase += stepDistance / Mathf.Max(LimbPose.CycleLength(CharacterBody3D.LegLength * height), 1e-4f) * 2f * Mathf.PI;
            }
            var state = LimbPose.StateOf(motor.IsGrounded, verticalSpeed);
            var k = 1f - Mathf.Exp(-Follow * dt);
            var grounded = state == LimbPose.State.Ground;
            swingScale = Mathf.Lerp(swingScale, grounded ? speed01 : 0f, k);
            air = Mathf.Lerp(air, grounded ? 0f : 1f, k);
            var walkPose = LimbPose.Evaluate(LimbPose.State.Ground, phase, swingScale, Time.time);
            if (!grounded)
            {
                // 空中の姿勢は、跳ぶ（腕を上げる）と落ちる（ばたばた）の間も、なめらかに移る
                var target = LimbPose.Evaluate(state, phase, 0f, Time.time);
                airPose.LeftArmSwing = Mathf.Lerp(airPose.LeftArmSwing, target.LeftArmSwing, k);
                airPose.RightArmSwing = Mathf.Lerp(airPose.RightArmSwing, target.RightArmSwing, k);
                airPose.LeftLegSwing = Mathf.Lerp(airPose.LeftLegSwing, target.LeftLegSwing, k);
                airPose.RightLegSwing = Mathf.Lerp(airPose.RightLegSwing, target.RightLegSwing, k);
                airPose.ArmRaise = Mathf.Lerp(airPose.ArmRaise, target.ArmRaise, k);
            }
            var pose = new LimbPose.Pose
            {
                LeftArmSwing = Mathf.Lerp(walkPose.LeftArmSwing, airPose.LeftArmSwing, air),
                RightArmSwing = Mathf.Lerp(walkPose.RightArmSwing, airPose.RightArmSwing, air),
                LeftLegSwing = Mathf.Lerp(walkPose.LeftLegSwing, airPose.LeftLegSwing, air),
                RightLegSwing = Mathf.Lerp(walkPose.RightLegSwing, airPose.RightLegSwing, air),
                ArmRaise = Mathf.Lerp(walkPose.ArmRaise, airPose.ArmRaise, air),
                Bob = walkPose.Bob * (1f - air),
            };
            lean = Mathf.Lerp(lean, grounded ? WalkLean * speed01 : 0f, k);

            // 腕：写真の左の腕（LeftArm）は +x 側。横へ上げるのは z 軸まわり（+x 側は +、-x 側は -）。前後は x 軸まわり（- で前へ）
            Turn(CharacterBody3D.LeftArm, pose.LeftArmSwing, pose.ArmRaise);
            Turn(CharacterBody3D.RightArm, pose.RightArmSwing, -pose.ArmRaise);
            Turn(CharacterBody3D.LeftLeg, pose.LeftLegSwing, 0f);
            Turn(CharacterBody3D.RightLeg, pose.RightLegSwing, 0f);

            // 頭・胴・腕は、上下にゆれる（足はそのまま。足が地面からはなれないように）
            var lift = Vector3.up * (pose.Bob * height);
            for (var i = CharacterBody3D.Head; i <= CharacterBody3D.RightArm; i++)
            {
                joints[i].localPosition = rest[i] + lift;
            }
            // 歩くときは、上の体（頭・胴・肩）を、腰のあたりを中心に少し前へ傾ける（+z が前）。肩も一緒に動かして、腕が離れないようにする
            var hip = new Vector3(0f, rest[CharacterBody3D.LeftLeg].y, 0f);
            var tilt = Quaternion.Euler(lean, 0f, 0f);
            for (var i = CharacterBody3D.Head; i <= CharacterBody3D.RightArm; i++)
            {
                joints[i].localPosition = hip + tilt * (joints[i].localPosition - hip);
            }
            joints[CharacterBody3D.Head].localRotation = tilt;
            joints[CharacterBody3D.Torso].localRotation = tilt;
        }

        void Turn(int index, float swing, float raise)
        {
            joints[index].localRotation = Quaternion.Euler(-swing, 0f, raise);
        }
    }
}
