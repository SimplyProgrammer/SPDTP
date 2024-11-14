
/**
* Used for time intervals and timeouts...
*/
public class AsyncTimer
{
	public readonly int TICK_RATE = 500;

	protected int timeoutPeriod, timeoutCount = 0, resetCount = 0;
	protected int remaining;

	protected Action callback;
	protected Thread timerThread;
	protected bool isRunning;

	public AsyncTimer(Action callback, int timeoutPeriod)
	{
		this.timeoutPeriod = timeoutPeriod;
		this.callback = callback;

		remaining = timeoutPeriod;
	}

	public AsyncTimer start()
	{
		if (isRunning)
			return this; 

		timerThread = new Thread(loop)
		{
			IsBackground = true
		};
		timerThread.Start();

		timeoutCount = resetCount = 0;
		isRunning = true;
		return this;
	}

	public void stop()
	{
		isRunning = false;
		timerThread?.Join();
	}

	public AsyncTimer restart(int remaining = -1, int refreshTimeoutCount = 0)
	{
		this.remaining = remaining < 0 ? timeoutPeriod : remaining;

		resetCount++;
		if (refreshTimeoutCount >= 0)
			timeoutCount = refreshTimeoutCount;

		return this;
	}

	public AsyncTimer setTimeout(Action callback, int timeoutPeriod)
	{
		this.timeoutPeriod = timeoutPeriod;
		this.callback = callback;

		return this;
	}

	public AsyncTimer setTimeout(int timeoutPeriod)
	{
		this.timeoutPeriod = timeoutPeriod;
		if (timeoutPeriod < remaining)
			remaining = timeoutPeriod;

		return this;
	}

	protected void loop()
	{
		while (isRunning)
		{
			Thread.Sleep(TICK_RATE);
			if (!isRunning)
				break;

			remaining -= TICK_RATE;
			if (remaining < 1)
			{
				timeoutCount++;

				callback.Invoke();
				remaining = timeoutPeriod;
			}
		}
	}

	public int getTimeoutCount()
	{
		return timeoutCount;
	}

	public int getResetCount()
	{
		return resetCount;
	}
}