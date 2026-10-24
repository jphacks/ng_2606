using System.Collections;
using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeskStage.Gameplay.PlayTests
{
    /// <summary>
    /// 小さな世界での物理の確かめ（C_gameplay.md の Step 8）。
    /// 表の項目を、テストからキャラを動かして確かめる。見た目の揺れなどは人が練習用シーンで確かめる。
    /// 箱はどれも幅・奥行き 8cm で、手前の面が z = 0（ずれる場合は個別に書く）。
    /// </summary>
    public class SmallWorldPhysicsTests
    {
        const float BoxWidth = 0.08f;

        GameObject world;
        CharacterSettings settings;
        CharacterMotor motor;
        CharacterController controller;

        [SetUp]
        public void SetUp()
        {
            world = new GameObject("TestWorld");
            settings = ScriptableObject.CreateInstance<CharacterSettings>();
            // 床：幅60cm・奥行き60cm・厚さ1cm。上面が高さ0
            CreateBox(new Vector3(0f, -0.005f, 0f), new Vector3(0.6f, 0.01f, 0.6f));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(world);
            Object.Destroy(settings);
            // 次のテストの床と重ならないよう、消えるまで1フレーム待つ
            yield return null;
        }

        // ---- 表の項目 ----

        [UnityTest]
        public IEnumerator 止まって立つと浮かず沈まず震えない()
        {
            yield return Spawn(new Vector3(0f, 0f, -0.2f));
            float min = float.MaxValue, max = float.MinValue;
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                float y = motor.transform.position.y;
                min = Mathf.Min(min, y);
                max = Mathf.Max(max, y);
                yield return null;
            }
            Assert.Less(max - min, 0.0002f, "立っているだけで上下に揺れている");
            Assert.That(min, Is.InRange(-0.0005f, 0.002f), "床から浮いているか沈んでいる");
        }

        [UnityTest]
        public IEnumerator 床を歩き回っても揺れず引っかからない()
        {
            yield return Spawn(new Vector3(-0.1f, 0f, -0.1f));
            Vector2[] directions = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
            foreach (var dir in directions)
            {
                Vector3 start = motor.transform.position;
                float maxY = float.MinValue;
                motor.SetTestInput(dir);
                for (float t = 0f; t < 1f; t += Time.deltaTime)
                {
                    maxY = Mathf.Max(maxY, motor.transform.position.y);
                    yield return null;
                }
                float moved = Vector3.Distance(Flat(start), Flat(motor.transform.position));
                Assert.Greater(moved, 0.12f, $"{dir} の向きに歩くと引っかかった");
                Assert.Less(maxY, 0.002f, $"{dir} の向きに歩くと跳ねた");
            }
        }

        [UnityTest]
        public IEnumerator 高さ0_5cmの箱は歩いて登れる()
        {
            CreateStep(0.005f);
            yield return Spawn(new Vector3(0f, 0f, -0.05f));
            motor.SetTestInput(Vector2.up);
            yield return new WaitForSeconds(0.6f);
            Assert.Greater(motor.transform.position.z, 0.02f, "箱の前で止まった");
            Assert.Greater(motor.transform.position.y, 0.003f, "箱の上に乗っていない");
        }

        [UnityTest]
        public IEnumerator 高さ2cmの箱はジャンプで登れる() { return ClimbsOnto(0.02f); }

        [UnityTest]
        public IEnumerator 高さ4cmの箱はジャンプで登れる() { return ClimbsOnto(0.04f); }

        [UnityTest]
        public IEnumerator 高さ4_5cmの箱はジャンプで登れる() { return ClimbsOnto(0.045f); }

        [UnityTest]
        public IEnumerator 高さ5cmの箱はジャンプでも登れない() { return CannotClimb(0.05f); }

        [UnityTest]
        public IEnumerator 高さ8cmの箱はジャンプでも登れない() { return CannotClimb(0.08f); }

        [UnityTest]
        public IEnumerator 高さ2cmを足場にして6cmへ登れる()
        {
            CreateStep(0.02f);
            CreateBox(new Vector3(0f, 0.03f, BoxWidth * 1.5f), new Vector3(BoxWidth, 0.06f, BoxWidth));
            yield return Spawn(new Vector3(0f, 0f, -0.05f));
            yield return WalkForwardJumpingWhenBlocked(3f, 0.06f - 0.003f);
            Assert.Greater(motor.transform.position.y, 0.06f - 0.003f, "6cm の箱の上に乗っていない");
        }

        [UnityTest]
        public IEnumerator すき間1cmは落ちずに渡れる()
        {
            CreateGapPair(0.01f);
            yield return Spawn(new Vector3(0f, 0.02f, -0.04f));
            float minY = float.MaxValue;
            motor.SetTestInput(Vector2.up);
            // 0.6 秒で約 9cm 進み、向こうの箱の上で止まる
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                minY = Mathf.Min(minY, motor.transform.position.y);
                yield return null;
            }
            Assert.Greater(minY, 0.015f, "すき間に落ちた");
            Assert.Greater(motor.transform.position.z, 0.03f, "向こうの箱まで渡れていない");
        }

        [UnityTest]
        public IEnumerator すき間2cmは落ちてはさまらない()
        {
            CreateGapPair(0.02f);
            yield return Spawn(new Vector3(0f, 0.02f, -0.04f));
            motor.SetTestInput(Vector2.up);
            yield return new WaitForSeconds(0.5f);
            motor.SetTestInput(Vector2.zero);
            yield return new WaitForSeconds(0.5f);
            Assert.Less(motor.transform.position.y, 0.003f, "すき間の途中ではさまって、床まで落ちていない");
        }

        [UnityTest]
        public IEnumerator 箱の角を走っても引っかからない()
        {
            // 箱は x 0〜8cm。キャラ（太さ1.6cm）の中心を x -0.6cm に置き、体が箱の角に 2mm かかるように走る
            CreateBox(new Vector3(BoxWidth / 2f, 0.04f, BoxWidth / 2f), new Vector3(BoxWidth, 0.08f, BoxWidth));
            yield return Spawn(new Vector3(-0.006f, 0f, -0.05f));
            motor.SetTestInput(Vector2.up);
            yield return new WaitForSeconds(1f);
            Assert.Greater(motor.transform.position.z, 0.05f, "箱の角で止まった");
            Assert.Less(motor.transform.position.y, 0.003f, "箱の角で跳ねた");
        }

        [UnityTest]
        public IEnumerator 壁に向かって走り続けてもめり込まない()
        {
            CreateStep(0.08f);
            yield return Spawn(new Vector3(0f, 0f, -0.05f));
            motor.SetTestInput(Vector2.up);
            yield return new WaitForSeconds(2f);
            float front = motor.transform.position.z + settings.radius;
            Assert.LessOrEqual(front, 0.0005f, "箱の中にめり込んだ");
        }

        [UnityTest]
        public IEnumerator 床の端から出ると落ちる()
        {
            yield return Spawn(new Vector3(0f, 0f, 0.25f));
            motor.SetTestInput(Vector2.up);
            yield return new WaitForSeconds(1f);
            Assert.Less(motor.transform.position.y, -0.05f, "床の端から落ちていない");
        }

        [UnityTest]
        public IEnumerator 床の端から出た直後ならジャンプできる()
        {
            yield return Spawn(new Vector3(0f, 0f, 0.25f));
            motor.SetTestInput(Vector2.up);
            for (float t = 0f; t < 1f && controller.isGrounded; t += Time.deltaTime)
            {
                yield return null;
            }
            Assert.IsFalse(controller.isGrounded, "床の端から出ていない");

            // 出てから 0.05 秒後（猶予 0.1 秒の内側）に押す
            yield return new WaitForSeconds(0.05f);
            float yAtPress = motor.transform.position.y;
            motor.PressJumpForTest();
            float maxY = yAtPress;
            for (float t = 0f; t < 0.15f; t += Time.deltaTime)
            {
                yield return null;
                maxY = Mathf.Max(maxY, motor.transform.position.y);
            }
            Assert.Greater(maxY, yAtPress + 0.01f, "出た直後に押したのに跳ばなかった");
        }

        [UnityTest]
        public IEnumerator 登れる高さの上限を測る()
        {
            // 合否ではなく、数値を決めるための測定。4.0〜6.0cm を 0.25cm ごとに試し、結果をログに出す
            float highestClimbed = 0f;
            var log = new System.Text.StringBuilder("[Step 8] 登れる高さ：");
            for (float h = 0.04f; h <= 0.0601f; h += 0.0025f)
            {
                yield return ResetWorld();
                CreateStep(h);
                yield return Spawn(new Vector3(0f, 0f, -0.05f));
                yield return WalkForwardJumpingWhenBlocked(2f, h - 0.003f);
                bool climbed = motor.transform.position.y > h - 0.003f;
                if (climbed)
                {
                    highestClimbed = h;
                }
                log.Append($" {h * 100f:0.00}cm={(climbed ? "○" : "×")}");
            }
            Debug.Log(log.ToString());
            Debug.Log($"[Step 8] 登れた一番高い箱：{highestClimbed * 100f:0.00}cm（決まりでは 4.5cm まで）");
            Assert.Pass(log.ToString());
        }

        // ---- 手順 ----

        // 床だけの世界に作り直す
        IEnumerator ResetWorld()
        {
            Object.Destroy(world);
            yield return null;
            world = new GameObject("TestWorld");
            CreateBox(new Vector3(0f, -0.005f, 0f), new Vector3(0.6f, 0.01f, 0.6f));
        }

        IEnumerator ClimbsOnto(float height)
        {
            CreateStep(height);
            yield return Spawn(new Vector3(0f, 0f, -0.05f));
            yield return WalkForwardJumpingWhenBlocked(2f, height - 0.003f);
            Assert.Greater(motor.transform.position.y, height - 0.003f, $"高さ {height * 100f}cm の箱に登れなかった");
        }

        IEnumerator CannotClimb(float height)
        {
            CreateStep(height);
            yield return Spawn(new Vector3(0f, 0f, -0.05f));
            yield return WalkForwardJumpingWhenBlocked(2f, height - 0.003f);
            motor.SetTestInput(Vector2.zero);
            yield return new WaitForSeconds(0.5f);
            Assert.Less(motor.transform.position.y, 0.003f, $"高さ {height * 100f}cm の箱に登れてしまった");
        }

        // 前へ歩き続け、地面にいて前へ進めなくなったらジャンプを押す（子どもが段の前で跳ぶのと同じ）。
        // stopAboveY より高い所に着いたら止まる（箱の向こう側へ歩いて落ちないように）
        IEnumerator WalkForwardJumpingWhenBlocked(float seconds, float stopAboveY)
        {
            motor.SetTestInput(Vector2.up);
            float bestZ = motor.transform.position.z;
            float lastProgress = Time.time;
            float nextJumpAllowed = 0f;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                if (controller.isGrounded && motor.transform.position.y > stopAboveY)
                {
                    break;
                }
                float z = motor.transform.position.z;
                if (z > bestZ + 0.0005f)
                {
                    bestZ = z;
                    lastProgress = Time.time;
                }
                if (controller.isGrounded && Time.time - lastProgress > 0.05f && Time.time >= nextJumpAllowed)
                {
                    motor.PressJumpForTest();
                    nextJumpAllowed = Time.time + 0.3f;
                }
                yield return null;
            }
            motor.SetTestInput(Vector2.zero);
            yield return new WaitForSeconds(0.2f);
        }

        // キャラを置いて、落ち着くまで待つ
        IEnumerator Spawn(Vector3 feet)
        {
            var go = new GameObject("Player");
            go.SetActive(false);
            go.layer = Layers.Player;
            go.transform.SetParent(world.transform, false);
            go.transform.position = feet + Vector3.up * 0.001f;
            controller = go.AddComponent<CharacterController>();
            motor = go.AddComponent<CharacterMotor>();
            motor.settings = settings;
            go.SetActive(true);
            motor.SetTestInput(Vector2.zero);
            yield return new WaitForSeconds(0.3f);
        }

        // 手前の面が z = 0 の箱
        void CreateStep(float height)
        {
            CreateBox(new Vector3(0f, height / 2f, BoxWidth / 2f), new Vector3(BoxWidth, height, BoxWidth));
        }

        // 高さ2cmの箱を2つ。手前の箱は z -8〜0cm、向こうの箱は z gap〜gap+8cm
        void CreateGapPair(float gap)
        {
            var size = new Vector3(BoxWidth, 0.02f, BoxWidth);
            CreateBox(new Vector3(0f, 0.01f, -BoxWidth / 2f), size);
            CreateBox(new Vector3(0f, 0.01f, gap + BoxWidth / 2f), size);
        }

        void CreateBox(Vector3 center, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.layer = Layers.Terrain;
            go.transform.SetParent(world.transform, false);
            go.transform.position = center;
            go.transform.localScale = size;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
