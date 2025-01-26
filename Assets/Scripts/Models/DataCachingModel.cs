﻿
using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Qf.Models
{
    public class DataCachingModel : AbstractModel
    {
        public Dictionary<string,AudioClip> ResourceAudioDatas = new();
        protected override void OnInit()
        {
            ResourceAudioDatas = GetResourceAudioClipData().ToDictionary(key=>key.name,value=>value);
        }
        public List<AudioClip> GetListAudioClips() => ResourceAudioDatas.Values.ToList();
      
        List<AudioClip> GetResourceAudioClipData()
        {
            AudioClip[] ls = Resources.LoadAll<AudioClip>("Audios");
            return ls.ToList();
        }
        public AudioClip GetAudioClip(string Name)
        {
            if (ResourceAudioDatas.ContainsKey(Name))
                return ResourceAudioDatas[Name];
            else
                return null;
        }
    }
}