using Godot;
using System;
using System.ComponentModel;
using System.Diagnostics;
using CharacterEmotion = LandlessSkies.Core.IPortraitProvider.CharacterEmotion;


namespace LandlessSkies.Core;

[Tool]
[GlobalClass]
public abstract partial class CharacterCostume : Resource, IPortraitProvider, IUIObject {
	[Export] public string DisplayName { get; private set; } = "";
	public Texture2D? DisplayPortrait => PortraitNeutral;

	[Export] public Texture2D? PortraitNeutral { get; private set; }
	[Export] public Texture2D? PortraitDetermined { get; private set; }
	[Export] public Texture2D? PortraitHesitant { get; private set; }
	[Export] public Texture2D? PortraitShocked { get; private set; }
	[Export] public Texture2D? PortraitDisgusted { get; private set; }
	[Export] public Texture2D? PortraitMelancholic { get; private set; }
	[Export] public Texture2D? PortraitJoyous { get; private set; }



	public Texture2D? GetPortrait(CharacterEmotion emotion) {
		return emotion switch {
			CharacterEmotion.Neutral        => PortraitNeutral,
			CharacterEmotion.Determined     => PortraitDetermined,
			CharacterEmotion.Hesitant       => PortraitHesitant,
			CharacterEmotion.Shocked        => PortraitShocked,
			CharacterEmotion.Disgusted      => PortraitDisgusted,
			CharacterEmotion.Melancholic    => PortraitMelancholic,
			CharacterEmotion.Joyous         => PortraitJoyous,
			_ when Enum.IsDefined(emotion)  => throw new UnreachableException($"Case for {nameof(CharacterEmotion)} {emotion} not implemented."),
			_                               => throw new InvalidEnumArgumentException()
		};
	}

	public abstract CharacterModel Instantiate(Node3D root);
}