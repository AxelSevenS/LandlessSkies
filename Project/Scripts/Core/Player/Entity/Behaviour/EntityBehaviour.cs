namespace LandlessSkies.Core;

using Godot;
using System;

[GlobalClass]
public abstract partial class EntityBehaviour : Node, IInputReader {
	[Export] public Entity Entity = null!;



	public EntityBehaviour() : base() { }
	public EntityBehaviour(Entity entity) : base() {
		ArgumentNullException.ThrowIfNull(entity);

		Entity = entity;
		Entity.AddChildAndSetOwner(this);
	}


	public abstract Interactable? GetInteractionCandidate();
	public virtual void HandleInput(Entity entity, CameraController3D cameraController, InputDevice inputDevice) { }

	public virtual bool SetSpeed(MovementSpeed speed) => true;
	public virtual bool Move(Vector3 direction) => true;
	public virtual bool Jump(Vector3? target = null) => true;

	public virtual void Start(EntityBehaviour? previousBehaviour) {
		ProcessMode = ProcessModeEnum.Inherit;
	}
	public virtual void Stop() {
		ProcessMode = ProcessModeEnum.Disabled;
	}



	public enum MovementSpeed {
		Idle = 0,
		Walk = 1,
		Run = 2,
		Sprint = 3
	}
}