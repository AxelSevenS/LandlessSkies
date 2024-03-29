namespace  LandlessSkies;

using System.Linq;
using Godot;
using LandlessSkies.Core;


public abstract partial class InputDevice : Node {
	public bool DeviceConnected { get; protected set; } = false;

	public abstract float Sensitivity { get; }
	protected abstract StringName ActionSuffix { get; }


	protected virtual InputEvent ConvertEvent(InputEvent @event) => @event;
	protected abstract bool IsEventSupported(InputEvent @event);

	protected StringName GetActionName(StringName action) => $"{action}_{ActionSuffix}";


	public virtual bool IsActionPressed(StringName action) =>
		DeviceConnected && Input.IsActionPressed(GetActionName(action));


	public virtual bool IsActionJustPressed(StringName action) =>
		DeviceConnected && Input.IsActionJustPressed(GetActionName(action));

	public virtual bool IsActionJustReleased(StringName action) =>
		DeviceConnected && Input.IsActionJustReleased(GetActionName(action));


	public virtual float GetActionStrength(StringName action) =>
		DeviceConnected ? Input.GetActionStrength(GetActionName(action)) : 0f;

	public virtual float GetActionRawStrength(StringName action) =>
		DeviceConnected ? Input.GetActionRawStrength(GetActionName(action)) : 0f;


	public Vector2 GetVector(StringName negativeX, StringName positiveX, StringName negativeY, StringName positiveY/* , float deadzone = -1 */) =>
		DeviceConnected
		? new(
			- GetActionStrength(negativeX) + GetActionStrength(positiveX),
			- GetActionStrength(negativeY) + GetActionStrength(positiveY)
		)
		: Vector2.Zero;


	protected static void RebindInput(StringName actionName, float deadzone = 0.5f, params InputEvent[] events) {
		if (actionName.IsEmpty)
			return;

		if (InputMap.HasAction(actionName)) {
			InputMap.ActionEraseEvents(actionName);
		} else {
			InputMap.AddAction(actionName, deadzone);
		}

		for (int i = 0; i < events.Length; i++) {
			InputMap.ActionAddEvent(actionName, events[i]);
		}
	}

	protected static void BindInput(StringName actionName, float deadzone = 0.5f, params InputEvent[] events) {
		if (actionName.IsEmpty)
			return;

		if (!InputMap.HasAction(actionName)) {
			InputMap.AddAction(actionName, deadzone);
		}

		for (int i = 0; i < events.Length; i++) {
			InputMap.ActionAddEvent(actionName, events[i]);
		}
	}

	protected static void UnbindInput(StringName actionName) {
		if (actionName.IsEmpty)
			return;

		if (InputMap.HasAction(actionName)) {
			InputMap.EraseAction(actionName);
		}
	}

	public virtual void Connect() {
		if (DeviceConnected)
			return;

		foreach (StringName action in InputManager.BaseActions) {
			StringName newAction = GetActionName(action);

			RebindInput(newAction, InputMap.ActionGetDeadzone(action), [.. InputMap.ActionGetEvents(action).Where(IsEventSupported).Select(ConvertEvent)]);
		}

		DeviceConnected = true;
	}

	public virtual void Disconnect() {
		if (!DeviceConnected)
			return;

		foreach (StringName action in InputManager.BaseActions) {
			StringName newAction = GetActionName(action);

			UnbindInput(newAction);
		}
		DeviceConnected = false;
	}
}
