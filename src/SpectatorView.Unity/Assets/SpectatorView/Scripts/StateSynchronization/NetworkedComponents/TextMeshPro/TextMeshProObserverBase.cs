// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using TMPro;

namespace Microsoft.MixedReality.SpectatorView
{
    internal abstract class TextMeshProObserverBase : ComponentObserver
    {
        private bool needsUpdate = false;

        protected TMP_Text TextMeshObserver
        {
            get;
            set;
        }

        public static bool HasFlag(TextMeshProBroadcasterChangeType changeType, TextMeshProBroadcasterChangeType flag)
        {
            return (changeType & flag) == flag;
        }

        protected abstract void EnsureTextComponent();

        private static bool[] Unpack(byte value)
        {
            bool[] result = new bool[8];
            byte mask = 1;
            for (int i = 0; i < 8; i++)
            {
                result[i] = (value & mask) == mask;
                mask <<= 1;
            }

            return result;
        }

        public override void Read(INetworkConnection connection, BinaryReader message)
        {
            EnsureTextComponent();

            TextMeshProBroadcasterChangeType changeType = (TextMeshProBroadcasterChangeType)message.ReadByte();

            if (HasFlag(changeType, TextMeshProBroadcasterChangeType.Text))
            {
                TextMeshObserver.SetText(message.ReadString());
            }

            if (HasFlag(changeType, TextMeshProBroadcasterChangeType.FontAndPlacement))
            {
                AssetId fontId = message.ReadAssetId();
                TextMeshObserver.font = TextMeshProService.Instance.GetFont(fontId);

                bool[] values = Unpack(message.ReadByte());
                TextMeshObserver.autoSizeTextContainer = values[0];
                TextMeshObserver.enableAutoSizing = values[1];
                TextMeshObserver.enableCulling = values[2];
                TextMeshObserver.enabled = values[3];
                TextMeshObserver.enableKerning = values[4];
                TextMeshObserver.enableWordWrapping = values[5];
                TextMeshObserver.extraPadding = values[6];
                TextMeshObserver.ignoreRectMaskCulling = values[7];

                values = Unpack(message.ReadByte());
                TextMeshObserver.ignoreVisibility = values[0];
                TextMeshObserver.isOrthographic = values[1];
                TextMeshObserver.isOverlay = values[2];
                TextMeshObserver.isRightToLeftText = values[3];
                TextMeshObserver.isVolumetricText = values[4];
                TextMeshObserver.maskable = values[5];
                TextMeshObserver.overrideColorTags = values[6];
                TextMeshObserver.parseCtrlCharacters = values[7];

                values = Unpack(message.ReadByte());
                TextMeshObserver.richText = values[0];
                TextMeshObserver.tintAllSprites = values[1];
                TextMeshObserver.useMaxVisibleDescender = values[2];

                TextMeshObserver.alignment = (TextAlignmentOptions)message.ReadInt32();
                TextMeshObserver.alpha = message.ReadSingle();
                TextMeshObserver.color = message.ReadColor();
                TextMeshObserver.characterSpacing = message.ReadSingle();
                TextMeshObserver.characterWidthAdjustment = message.ReadSingle();
                TextMeshObserver.faceColor = message.ReadColor32();
                TextMeshObserver.firstVisibleCharacter = message.ReadInt32();
                TextMeshObserver.fontSize = message.ReadSingle();
                TextMeshObserver.fontSizeMax = message.ReadSingle();
                TextMeshObserver.fontSizeMin = message.ReadSingle();
                TextMeshObserver.fontStyle = (FontStyles)message.ReadInt32();
                TextMeshObserver.fontWeight = (FontWeight)message.ReadInt32();
                TextMeshObserver.horizontalMapping = (TextureMappingOptions)message.ReadByte();
                TextMeshObserver.lineSpacing = message.ReadSingle();
                TextMeshObserver.lineSpacingAdjustment = message.ReadSingle();
                TextMeshObserver.mappingUvLineOffset = message.ReadSingle();
                TextMeshObserver.margin = message.ReadVector4();
                TextMeshObserver.maxVisibleCharacters = message.ReadInt32();
                TextMeshObserver.maxVisibleLines = message.ReadInt32();
                TextMeshObserver.maxVisibleWords = message.ReadInt32();
                TextMeshObserver.outlineColor = message.ReadColor32();
                TextMeshObserver.outlineWidth = message.ReadSingle();
                TextMeshObserver.overflowMode = (TextOverflowModes)message.ReadByte();
                TextMeshObserver.pageToDisplay = message.ReadInt32();
                TextMeshObserver.paragraphSpacing = message.ReadSingle();
                TextMeshObserver.renderMode = (TextRenderFlags)message.ReadByte();
                TextMeshObserver.verticalMapping = (TextureMappingOptions)message.ReadByte();
                TextMeshObserver.wordWrappingRatios = message.ReadSingle();
                TextMeshObserver.wordSpacing = message.ReadSingle();

                needsUpdate = true;
            }
        }

        protected virtual void Update()
        {
            if (needsUpdate)
            {
                // Applying a font/forcing a mesh update on the same Update pass that the TextMeshObserver is created fails to display things correctly.
                // Therefore, we force a mesh update on the next Update pass to get things displaying correctly.
                TextMeshObserver.ForceMeshUpdate();
                needsUpdate = false;
            }
        }
    }
}