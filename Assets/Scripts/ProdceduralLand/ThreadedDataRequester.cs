using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class ThreadedDataRequester : MonoBehaviour {

	static ThreadedDataRequester instance;
	Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();

	void Awake() {
		instance = FindObjectOfType<ThreadedDataRequester> ();
	}

	public static void RequestData(Func<object> generateData, Action<object> callback) {
		ThreadStart threadStart = delegate {
			instance.DataThread (generateData, callback);
		};

		new Thread (threadStart).Start ();
	}

	void DataThread(Func<object> generateData, Action<object> callback) {
		object data = generateData ();
		lock (dataQueue) {
			dataQueue.Enqueue (new ThreadInfo (generateData,callback, data));
		}
	}


	void Update() {
		if (dataQueue.Count > 0) {
			for (int i = 0; i < dataQueue.Count; i++) {
				ThreadInfo threadInfo = dataQueue.Dequeue ();
				if (threadInfo.callback != null && threadInfo.parameter != null) {
					threadInfo.callback (threadInfo.parameter);
				} else {
					Debug.Log(threadInfo.callback);
					Debug.Log(threadInfo.parameter);
					RequestData (threadInfo.generateData, threadInfo.callback);
				}
			}
		}
	}

	struct ThreadInfo {
		public readonly Func<object> generateData;
		public readonly Action<object> callback;
		public readonly object parameter;

		public ThreadInfo (Func<object> generateData,Action<object> callback, object parameter)
		{
			this.generateData = generateData;
			this.callback = callback;
			this.parameter = parameter;
		}

	}
}