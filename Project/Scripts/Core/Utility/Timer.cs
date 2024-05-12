namespace SevenDev.Utility;

using Godot;

/// <summary>
/// Timer is used to measure how much time has passed since it was started
/// </summary>
public struct Timer {
	public float startTime = 0;
	public readonly float Duration => Time.GetTicksMsec() - startTime;



	public Timer() { }



	public void Start() {
		startTime = Time.GetTicksMsec();
	}


	public static implicit operator float(Timer timer) => timer.Duration;
}