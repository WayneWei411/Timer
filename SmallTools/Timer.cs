using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
public class Timer : MonoBehaviour
{
    private Dictionary<string, ICountDown> allCountDowns = new Dictionary<string, ICountDown>();
    private CountDownFactory countDownFactory = new CountDownFactory();
    private readonly char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
    private static readonly object instanceLock = new object();
    public static Timer instance;
    public static Timer Instance {
        get {
            if (instance) {
                return instance;
            }
            lock(instanceLock) {
                instance = FindObjectOfType<Timer>();
                if (!instance) {
                    GameObject timerObj = GameObject.Find("Timer");
                    if(!timerObj) {
                        timerObj = new GameObject("Timer");
                    }
                    instance = timerObj.AddComponent<Timer>();
                    DontDestroyOnLoad(timerObj);
                }
            }
            return instance;    
        }
    }

    private void FixedUpdate() {
        for(int i = 0; i < allCountDowns.Count; i++) {
            ICountDown countDown = allCountDowns.Values.ElementAt(i);
            if(!countDown.IsFullyComplete()) {
                countDown.CountDown(Time.fixedDeltaTime);
            }
            countDown.UpdateStatus();
            if(countDown.IsFullyComplete() && countDown.actionAfterCountDown != null) {
                countDown.actionAfterCountDown();
            }
        }
    }

    public void StartCounting(TimerMode mode, string id, float time) {
        var countDown = countDownFactory.Produce(mode, id, time);
        allCountDowns[id] = countDown;    
    }

    public void StartCounting(TimerMode mode, string id, float time, int counts) {
        var countDown = countDownFactory.Produce(mode, id, time, counts);
        allCountDowns[id] = countDown;    
    }

    public void StartCounting(TimerMode mode, string id, float time, Action action) {
        var countDown = countDownFactory.Produce(mode, id, time);
        countDown.actionAfterCountDown = action;
        allCountDowns[id] = countDown;
    }

    public void InsertTimer(float time, Action action) {
        string id = RandomID();
        while(allCountDowns.ContainsKey(id)) { 
            id = RandomID();
        }
        Action actionAfterCountDown = () => { action(); allCountDowns.Remove(id);};
        StartCounting(TimerMode.one_time, id, time, actionAfterCountDown);
    }

    public bool IsOneCountingComplete(string id) {
        if(allCountDowns.ContainsKey(id)) {    
            return allCountDowns[id].IsOneCountingComplete();
        }
        else {
            Debug.LogWarning("there's no object with name: " + id + " in the complete list");
            return false;
        }
    }

    public void StopTimer(string id) {
        allCountDowns.Remove(id);
    }

    private string RandomID() {
        int randInt = Random.Range(1, 26);
        StringBuilder builder = new StringBuilder("", randInt);
        for(int i = 0; i < randInt; i++) {
            int c = Random.Range(0, 51);
            builder.Append(alphabet[c]); 
        }
        return builder.ToString();
    }
}

public class CountDownFactory{

    public ICountDown Produce(TimerMode mode, string id, float time) {
        if(mode == TimerMode.one_time) {
            CountDown_Single countDown = new CountDown_Single(id, time);
            return countDown;
        }
        else if(mode == TimerMode.repeat){
            CountDown_Repeated countDown = new CountDown_Repeated(id, time);
            return countDown;
        }
        return new CountDown_Empty(id, time);
    }

    public ICountDown Produce(TimerMode mode, string id, float time, int counts) { 
        if(mode == TimerMode.multi_time) {
            CountDown_Multi countDown = new CountDown_Multi(id, time, counts);
            return countDown;
        }
        return new CountDown_Empty(id, time, counts);
    }
}

public interface ICountDown {
    public string countDownID { get; set; }
    public TimerStatus status { get; set; }
    public Action actionAfterCountDown { get; set; }
    public void CountDown(float t);
    public void UpdateStatus();
    public bool IsOneCountingComplete();
    public bool IsFullyComplete();
}

public class CountDown_Single : ICountDown {
    public string countDownID { get; set; }
    public TimerStatus status { get; set; } = TimerStatus.beginning;
    public Action actionAfterCountDown { get; set; }
    private float remainTime;

    public CountDown_Single(string id, float time) {
        countDownID = id;
        remainTime = time;
    }

    public void CountDown(float timeCounted) {
        remainTime -= timeCounted;
    }

    public void UpdateStatus() {
        if(remainTime > 0) {
            status = TimerStatus.inProcess;
        }
        else {
            status = TimerStatus.fullyComplete;   
        }
    }

    public bool IsOneCountingComplete() {
        return remainTime <= 0;
    }

    public bool IsFullyComplete() {
        return status == TimerStatus.fullyComplete; 
    }
}

public class CountDown_Multi : ICountDown {
    public string countDownID { get; set; }
    public TimerStatus status { get; set; } = TimerStatus.beginning;
    public Action actionAfterCountDown { get; set; }
    private float timeAmount;
    private float remainTime;
    private int currentCounts = 0;
    private int lastCountIndex = 0;
    public int remainCounts;

    public CountDown_Multi(string id, float time, int counts) {
        countDownID = id;
        timeAmount = time;
        remainTime = time;
        remainCounts = counts;
    }

    public void CountDown(float timeCounted) {
        remainTime -= timeCounted;
    }

    public void UpdateStatus() {
        if(remainTime > 0) {
            status = TimerStatus.inProcess;
        }
        else { 
            if(remainCounts > 0) {
                remainCounts--;
                currentCounts++;
                remainTime = timeAmount;
                status = TimerStatus.inProcess;
            }
            else {
                status = TimerStatus.fullyComplete;
            }
        }
    }

    public bool IsOneCountingComplete() {
        if(currentCounts - lastCountIndex >= 1) {
            lastCountIndex = currentCounts;
            return true;
        }
        return false;
    }

    public bool IsFullyComplete() {
        return status == TimerStatus.fullyComplete;
    }
}

public class CountDown_Repeated : ICountDown {
    public string countDownID { get; set; }
    public TimerStatus status { get; set; } = TimerStatus.beginning;
    public Action actionAfterCountDown { get; set; }
    private float timeAmount;
    private float remainTime;
    private int currentCounts = 0;
    private int lastCountIndex = 0;

    public CountDown_Repeated(string id, float time) { 
        countDownID = id;
        timeAmount = time;
        remainTime = time;
    }

    public void CountDown(float timeCounted) {
        remainTime -= timeCounted;
    }

    public void UpdateStatus() {   
        if(remainTime < 0) {
            remainTime = timeAmount;
            currentCounts++;
        }
        status = TimerStatus.inProcess;
    }

    public bool IsOneCountingComplete() {
        if(currentCounts - lastCountIndex >= 1) {
            lastCountIndex = currentCounts;
            return true;
        }
        return false;
    }

    public bool IsFullyComplete() {
        return status == TimerStatus.fullyComplete;
    }
}

public class CountDown_Empty : ICountDown {
    public string countDownID { get; set; }
    public TimerStatus status { get; set; } = TimerStatus.beginning;
    public Action actionAfterCountDown { get; set; }
    public int remainCounts;

    public CountDown_Empty(string id, float time) {
        Debug.LogWarning("This countdown is empty , try to assign a new countdown or check the parameter counts is correct");
    }

    public CountDown_Empty(string id, float time, int counts) {
        Debug.LogWarning("This countdown is empty , try to assign a new countdown or check the parameter counts is correct");
    }

    public void CountDown(float timeCounted) {
        Debug.LogWarning("This countdown is empty , try to assign a new countdown or check the parameter counts is correct");
    }

    public void UpdateStatus() {
        Debug.LogWarning("This countdown is empty , try to assign a new countdown or check the parameter counts is correct");
    }

    public bool IsOneCountingComplete() {
        Debug.LogWarning("This countdown is empty , try to assign a new countdown or check the parameter counts is correct");
        return false;
    }

    public bool IsFullyComplete() {
        Debug.LogWarning("This countdown is empty , try to assign a new countdown or check the parameter counts is correct");
        return status == TimerStatus.fullyComplete;
    }
}

public enum TimerMode {
    one_time,
    multi_time,
    repeat
}

public enum TimerStatus {
    beginning,
    inProcess,
    fullyComplete
}
