using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.UI.Tests
{
    public class ScreenRouterTests
    {
        GameObject root;
        GameObject[] panels;
        ScreenRouter router;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("router");
            router = root.AddComponent<ScreenRouter>();
            panels = new GameObject[7];
            for (int i = 0; i < panels.Length; i++)
            {
                panels[i] = new GameObject("screen" + i);
            }
            router.SetScreens(panels);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var p in panels)
            {
                Object.DestroyImmediate(p);
            }
            Object.DestroyImmediate(root);
            StageContext.Mode = AppMode.Menu;
        }

        [Test]
        public void 指定した画面だけが表示される()
        {
            router.Show(ScreenId.Scan);
            for (int i = 0; i < panels.Length; i++)
            {
                Assert.AreEqual(i == (int)ScreenId.Scan, panels[i].activeSelf, "画面 " + i);
            }
        }

        [Test]
        public void 切り替えると場面が書き換わる()
        {
            router.Show(ScreenId.Check);
            Assert.AreEqual(AppMode.Check, StageContext.Mode);
            router.Show(ScreenId.List);
            Assert.AreEqual(AppMode.Menu, StageContext.Mode);
        }
    }
}
