﻿using Qf.Models;
using QFramework;
using System.Collections;
using UnityEngine;

namespace Qf.Querys.AudioEdit
{
    public class QueryAudioEditLoadAudio : AbstractQuery<AudioClip>
    {
        DataCachingModel cachingModel;
        string AudioClipName;
        public QueryAudioEditLoadAudio(string AudioClipName)
        {
            this.AudioClipName = AudioClipName;
        }
        protected override AudioClip OnDo()
        {
            cachingModel = this.GetModel<DataCachingModel>();
            foreach(var i in cachingModel.ResourceAudioDatas.Keys)
            {
                if (i.Equals(AudioClipName))
                {
                    return cachingModel.ResourceAudioDatas[i];
                }
            }
            return null;
        }
    }
}