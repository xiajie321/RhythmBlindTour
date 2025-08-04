using System;
using System.Collections.Generic;
using System.Linq;
using QFramework;
using TMPro;
using UnityEngine;

namespace TestOffset
{
    public class Offset : Architecture<Offset>
    {
        protected override void Init()
        {
            this.RegisterModel(new OffsetModel());
        }
    }

    public class OffsetController : MonoBehaviour, IController
    {
        //view
        private TMP_Text current;
        private TMP_Text total;

        public AudioSource audioSource;
        private OffsetModel offsetModel;

        private bool isPlaying = false;
        [SerializeField] private BindableList<double> inputTimes; //相对时间
        private double startTime; //绝对时间
        public double delayTime;

        private void Start()
        {
            //view
            current = GameObject.Find("current").GetComponent<TMP_Text>();
            total = GameObject.Find("total").GetComponent<TMP_Text>();
            //model
            offsetModel = this.GetModel<OffsetModel>();

            inputTimes = new();

            inputTimes.OnCountChanged.Register(i =>
            {
                if (i >= 7)
                {
                    audioSource.Stop();
                    isPlaying = false;
                    var offset = CalculateOffset();
                    total.text = $"{offset:N0}ms";
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            inputTimes.OnAdd.Register((index, v) => { current.text = $"{index}：{v:F0}ms"; });
        }

        private void Update()
        {
            if (isPlaying)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    double input = AudioSettings.dspTime - startTime;
                    input %= 2;
                    var curOffset = input < 1 ? input : input - 2d;
                    curOffset *= 1000;
                    inputTimes.Add(curOffset);
                }
            }
        }

        private double CalculateOffset()
        {
            var v = inputTimes.Sum() / inputTimes.Count;
            int offset = (int)Math.Floor(v);
            offsetModel.offset = offset;
            return offset;
        }

        public void AudioPlayScheduled()
        {
            isPlaying = true;
            inputTimes.Clear();
            double time;
            audioSource.PlayScheduled(time = AudioSettings.dspTime + delayTime);
            Debug.Log($"开始时间：{time}");
            startTime = time;
        }

        public IArchitecture GetArchitecture()
        {
            return Offset.Interface;
        }
    }
}