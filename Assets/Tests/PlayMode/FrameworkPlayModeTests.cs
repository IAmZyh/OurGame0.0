using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Spotlight.Tests.PlayMode
{
    public sealed class FrameworkPlayModeTests
    {
        [UnityTest]
        public IEnumerator DuplicateBootstrap_KeepsOnlyOnePersistentRoot()
        {
            if (GameBootstrap.Instance != null)
            {
                Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }

            GameObject first = CreateBootstrap("First", false);
            GameObject second = CreateBootstrap("Second", false);
            yield return null;

            GameBootstrap[] bootstraps = Object.FindObjectsOfType<GameBootstrap>(true);
            Assert.That(bootstraps, Has.Length.EqualTo(1));
            Assert.That(GameBootstrap.Instance, Is.Not.Null);

            Object.Destroy(first);
            if (second != null)
            {
                Object.Destroy(second);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameplayGreybox_RoutesUiAndWorldByFixedPriority()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
            yield return null;
            Physics2D.SyncTransforms();
            Canvas.ForceUpdateCanvases();

            InputRouter router = Object.FindObjectOfType<InputRouter>();
            Camera camera = Camera.main;
            PlayerMovement player = Object.FindObjectOfType<PlayerMovement>();
            CameraController cameraController = Object.FindObjectOfType<CameraController>();
            InteractionProbe probe = Object.FindObjectOfType<InteractionProbe>();
            RectTransform uiPanel = GameObject.Find("InputBlockingPanel").GetComponent<RectTransform>();

            Assert.That(router, Is.Not.Null);

            Vector2 uiPoint = RectTransformUtility.WorldToScreenPoint(null, uiPanel.position);
            Assert.That(router.RoutePointerDown(uiPoint), Is.EqualTo(InputRouteResult.UI));

            int beforeInteraction = probe.InteractionCount;
            Vector2 probePoint = camera.WorldToScreenPoint(probe.transform.position);
            Assert.That(router.RoutePointerDown(probePoint), Is.EqualTo(InputRouteResult.Interaction));
            Assert.That(probe.InteractionCount, Is.EqualTo(beforeInteraction + 1));

            Vector2 walkablePoint = camera.WorldToScreenPoint(new Vector3(0f, -2f, 0f));
            Assert.That(router.RoutePointerDown(walkablePoint), Is.EqualTo(InputRouteResult.Movement));

            router.SetInputEnabled(false);
            float playerTargetBeforeLockedClick = player.TargetX;
            Assert.That(router.RoutePointerDown(walkablePoint), Is.EqualTo(InputRouteResult.Disabled));
            Assert.That(player.TargetX, Is.EqualTo(playerTargetBeforeLockedClick));

            router.SetInputEnabled(true);
            float cameraStartX = camera.transform.position.x;
            player.SetTargetX(8f);
            float followDeadline = Time.realtimeSinceStartup + 5f;
            while (player.transform.position.x < 7.9f && Time.realtimeSinceStartup < followDeadline)
            {
                yield return null;
            }

            for (int index = 0; index < 20; index++)
            {
                yield return null;
            }

            Assert.That(player.transform.position.x, Is.EqualTo(8f).Within(0.1f));
            Assert.That(camera.transform.position.x, Is.GreaterThan(cameraStartX));
            Assert.That(camera.transform.position.x, Is.InRange(cameraController.MinX, cameraController.MaxX));
        }

        [UnityTest]
        public IEnumerator SceneLoader_RejectsSecondRequestWhileLoading()
        {
            GameObject loaderObject = new GameObject("SceneLoaderTest");
            Object.DontDestroyOnLoad(loaderObject);
            SceneLoader loader = loaderObject.AddComponent<SceneLoader>();

            Assert.That(loader.RequestLoad("Gameplay", "PlayModeTest"), Is.True);
            Assert.That(loader.IsLoading, Is.True);
            LogAssert.Expect(LogType.Warning,
                "[SceneLoader] Rejected 'Gameplay' from 'DuplicateRequest': a scene is already loading.");
            Assert.That(loader.RequestLoad("Gameplay", "DuplicateRequest"), Is.False);

            float deadline = Time.realtimeSinceStartup + 15f;
            while (loader != null && loader.IsLoading && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(loader, Is.Not.Null);
            Assert.That(loader.IsLoading, Is.False);
            Object.Destroy(loaderObject);
        }

        private static GameObject CreateBootstrap(string name, bool autoStart)
        {
            GameObject gameObject = new GameObject(name);
            RunSession session = gameObject.AddComponent<RunSession>();
            SceneLoader loader = gameObject.AddComponent<SceneLoader>();
            GameFlowController flow = gameObject.AddComponent<GameFlowController>();
            GameBootstrap bootstrap = gameObject.AddComponent<GameBootstrap>();
            flow.Initialize(session, loader);
            bootstrap.Configure(session, loader, flow, autoStart);
            return gameObject;
        }
    }
}
