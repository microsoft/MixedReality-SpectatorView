using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Video;

namespace Microsoft.MixedReality.SpectatorView.Tests
{
    [UnityPlatform(RuntimePlatform.WindowsEditor)]
    public class CompositionManagerTests : CompositorTestsBase
    {
        const double recordTimeInSeconds = 5.0;
        const int compositeVideoWidth = 1920;
        const int compositeVideoHeight = 1080;
        const int quadVideoWidth = compositeVideoWidth * 2;
        const int quadVideoHeight = compositeVideoHeight * 2;
        const double recordTimeAcceptableErrorInSeconds = 1.3;
        const int numVideos = 3;

        [UnityTest]
        public IEnumerator TextureInitializationBlackmagicDesignTest()
        {
            yield return CompositionManager.CaptureDevice = FrameProviderDeviceType.BlackMagic;
            Assert.AreEqual(CompositionManager.CaptureDevice, FrameProviderDeviceType.BlackMagic, "Set CaptureDevice as BlackMagic");
            yield return AssertTexturesInitialize("Blackmagic Design");
        }

        [UnityTest]
        public IEnumerator TextureInitializationElgatoTest()
        {
            yield return CompositionManager.CaptureDevice = FrameProviderDeviceType.Elgato;
            Assert.AreEqual(CompositionManager.CaptureDevice, FrameProviderDeviceType.Elgato, "Set CaptureDevice as Elgato");
            yield return AssertTexturesInitialize("Elgato");
        }

        [UnityTest]
        public IEnumerator RecordCompositeVideoTest()
        {
            yield return SetupCompositeRecording();

            float startTime = Time.time;

            bool startedRecording = CompositionManager.TryStartRecording(out var videoFilePath);
            Assert.IsTrue(startedRecording, "Starting recording succeeded.");

            Debug.Log($"Recording file: {videoFilePath}");
            filesToDelete.Add(videoFilePath);
            while (Time.time - startTime < recordTimeInSeconds)
            {
                yield return null;
            }
            CompositionManager.StopRecording();
            yield return null;

            yield return AssertVideoFileParams(videoFilePath, compositeVideoWidth, compositeVideoHeight, recordTimeInSeconds);
        }

        [UnityTest]
        public IEnumerator RecordMultipleCompositeVideosTest()
        {
            yield return SetupCompositeRecording();

            for (int n = 0; n < numVideos; n++)
            {
                float startTime = Time.time;

                Debug.Log($"Recording Composite Video: {n}");
                bool startedRecording = CompositionManager.TryStartRecording(out var videoFilePath);
                Assert.IsTrue(startedRecording, "Starting recording succeeded.");

                Debug.Log($"Recording file: {videoFilePath}");
                filesToDelete.Add(videoFilePath);
                while (Time.time - startTime < recordTimeInSeconds)
                {
                    yield return null;
                }
                CompositionManager.StopRecording();
                yield return null;

                yield return AssertVideoFileParams(videoFilePath, compositeVideoWidth, compositeVideoHeight, recordTimeInSeconds);
            }
        }

        [UnityTest]
        public IEnumerator RecordQuadVideoTest()
        {
            yield return SetupQuadRecording();

            float startTime = Time.time;

            bool startedRecording = CompositionManager.TryStartRecording(out var videoFilePath);
            Assert.IsTrue(startedRecording, "Starting recording succeeded.");

            Debug.Log($"Recording file: {videoFilePath}");
            filesToDelete.Add(videoFilePath);
            while (Time.time - startTime < recordTimeInSeconds)
            {
                yield return null;
            }
            CompositionManager.StopRecording();
            yield return null;

            yield return AssertVideoFileParams(videoFilePath, quadVideoWidth, quadVideoHeight, recordTimeInSeconds);
        }

        [UnityTest]
        public IEnumerator RecordMultipleQuadVideosTest()
        {
            yield return SetupQuadRecording();

            for (int n = 0; n < numVideos; n++)
            {
                float startTime = Time.time;

                Debug.Log($"Recording Quad Video: {n}");
                bool startedRecording = CompositionManager.TryStartRecording(out var videoFilePath);
                Assert.IsTrue(startedRecording, "Starting recording succeeded.");

                Debug.Log($"Recording file: {videoFilePath}");
                filesToDelete.Add(videoFilePath);
                while (Time.time - startTime < recordTimeInSeconds)
                {
                    yield return null;
                }
                CompositionManager.StopRecording();
                yield return null;

                yield return AssertVideoFileParams(videoFilePath, quadVideoWidth, quadVideoHeight, recordTimeInSeconds);
            }

        }

        private IEnumerator AssertVideoFileParams(string filePath, int width, int height, double expectedDuration)
        {
            VideoPlayer player = CompositionManager.gameObject.AddComponent<VideoPlayer>();
            Assert.NotNull(player, "Player is not null.");
            Debug.Log($"Loading video file: {filePath}");
            player.url = filePath;
            WaitForSeconds waitTime = new WaitForSeconds(1);
            player.Prepare();
            while (!player.isPrepared)
            {
                Debug.Log("Player is not yet prepared...");
                yield return waitTime;
            }

            player.Play();
            while (!player.isPlaying)
            {
                Debug.Log("waitTime is not yet playing...");
                yield return null;
            }

            Debug.Log($"Asserting Video File Params, File:{player.url}, Length:{player.length}, Width:{player.width}, Height:{player.height}");
            double minTime = expectedDuration - recordTimeAcceptableErrorInSeconds;
            double maxTime = expectedDuration + recordTimeAcceptableErrorInSeconds;
            Assert.IsTrue(
                (player.length > minTime) &&
                (player.length < maxTime),
                $"Video length expected value:({minTime},{maxTime}), actual:{player.length}");
            Assert.IsTrue(
                (player.width == width) &&
                (player.height == height),
                $"Video dimensions were expected value: {width}x{height}, actual:{player.width}x{player.height}");

            player.Stop();
            while (player.isPlaying)
            {
                Debug.Log("Player is not yet stopped...");
                yield return waitTime;
            }
            Component.Destroy(player);
        }

        private IEnumerator SetupCompositeRecording()
        {
            yield return CompositionManager.CaptureDevice = FrameProviderDeviceType.BlackMagic;
            Assert.AreEqual(CompositionManager.CaptureDevice, FrameProviderDeviceType.BlackMagic, "Set CaptureDevice as Blackmagic");

            yield return CompositionManager.VideoRecordingLayout = VideoRecordingFrameLayout.Composite;
        }

        private IEnumerator SetupQuadRecording()
        {
            yield return CompositionManager.CaptureDevice = FrameProviderDeviceType.BlackMagic;
            Assert.AreEqual(CompositionManager.CaptureDevice, FrameProviderDeviceType.BlackMagic, "Set CaptureDevice as Blackmagic");

            yield return CompositionManager.VideoRecordingLayout = VideoRecordingFrameLayout.Quad;
        }
    }
}