using LitJson;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ASE_to_Unity {

    /// <summary>
    /// A class containing data pulled from a .ase file
    /// </summary>
    public class AseData {
        public Vector2 dim;
        public string name;
        public int[] durations;
        public List<Clip> clips;
        public int FrameCount { get { return durations.Length; } }
        public Clip this[string clipName] {
            get {
                foreach (Clip c in clips)
                    if (c.name.ToLower().Equals(clipName.ToLower()))
                        return c;
                return null;
            }
        }

        /// <summary>
        /// A class containing data for an animation clip found in a .ase file
        /// </summary>
        public class Clip {
            public string name;
            public int start, end, sampleRate;
            public bool dynamicRate, looping;
            private AseData owner;

            public float l0;
            public int[] samples;
            public int Count { get { return end - start + 1; } }
            public int[] Frames { get { return owner.durations.SubArray(start, Count); } }

            public Clip(AseData owner, string name, int s, int e, bool looping) {
                this.owner = owner;
                this.name = name;
                this.looping = looping;
                start = s;
                end = e;

                samples = new int[Count + 1];
                Init();
            }

            /// <summary>
            /// gets the sample index for the given frame
            /// </summary>
            /// <param name="i"></param>
            /// <returns></returns>
            public int this[int i] { get { return samples[i]; } }

            /// <summary>
            /// calculates animation relavent data
            /// </summary>
            public void Init() {
                dynamicRate = false;
                foreach (int i in Frames)
                    if (i != Frames[0]) {
                        dynamicRate = true;
                        break;
                    }

                l0 = dynamicRate ? Anim_Import.GCD(Frames) : Frames[0]; // frame duration
                if (l0 > 1000) {
                    // Unity has a limitation that the samplerate must be an integer above 1
                    dynamicRate = true;
                    l0 = 10;
                }

                sampleRate = (int)Math.Round(1000f / l0);

                // for each frame, place event at sample index
                int end = (dynamicRate) ? Count + 1 : Count;
                for (int i0 = 0; i0 < end; i0++) {
                    samples[i0] = i0;
                    if (dynamicRate) {
                        samples[i0] = (int)(Anim_Import.sum(Frames, i0) / l0);
                        if ((i0 == Count)) samples[i0]--;
                    }
                }
            }
        }

        /// <summary>
        /// Reads animation data from an extracted json file.
        /// </summary>
        /// <param name="file"> location of the json file </param>
        /// <returns></returns>
        public static AseData ReadFromJSON(string file) {
            AseData anim = new AseData();
            JsonData dat = JsonMapper.ToObject(File.ReadAllText(file));
            JsonData frames = dat["frames"];
            JsonData tags = dat["meta"]["frameTags"];
            JsonData size = frames[0]["sourceSize"];
            anim.clips = new List<Clip>();

            anim.durations = new int[frames.Count];
            for (int i = 0; i < frames.Count; i++) {
                anim.durations[i] = int.Parse(frames[i]["duration"].ToString());
            }

            anim.dim = new Vector2(float.Parse(size["w"].ToString()),
                float.Parse(size["h"].ToString()));

            // load loop names
            if (tags.Count == 0) {
                anim.clips.Add(new Clip(anim, "base", 0, frames.Count - 1, true));
            } else {
                for (int i = 0; i < tags.Count; i++) {
                    Clip clip = new Clip(anim,
                        AseUtils.UppercaseFirst(tags[i]["name"].ToString()),
                        int.Parse(tags[i]["from"].ToString()),
                        int.Parse(tags[i]["to"].ToString()),
                        tags[i]["direction"].ToString().Equals("forward"));
                    //Debug.Log(clip.name + " looping: " + clip.looping);
                    anim.clips.Add(clip);
                }
            }

            return anim;
        }
    }
}
