using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    [InitializeOnLoad]
    public static class TerraceJumpCheckCI
    {
        private const string Armed="GOAT.TerraceJumpCheck";
        private static ScenicGoatCourse course;
        private static GoatController goat;
        private static RespawnController life;
        private static Rigidbody body;
        private static int index;
        private static bool aligned;
        private static float started;
        private static float settledAt=-1;
        private static bool active,released,airborne;
        private static Vector3 heading;
        private static bool charging;
        private static int frame,lastFrame=-1;
        private static RenderTexture videoTarget;
        private static Texture2D videoTexture;
        private static void Record()
        {
            if(!SessionState.GetBool(Armed+".Record",false)||lastFrame==Time.frameCount)return;
            lastFrame=Time.frameCount;
            if(!videoTarget){videoTarget=new RenderTexture(960,540,24);videoTexture=new Texture2D(960,540,TextureFormat.RGB24,false);}
            var camera=Camera.main;var previous=camera.targetTexture;var activeTarget=RenderTexture.active;
            try
            {
                camera.targetTexture=videoTarget;camera.Render();RenderTexture.active=videoTarget;
                videoTexture.ReadPixels(new Rect(0,0,960,540),0,0);videoTexture.Apply();
                File.WriteAllBytes("Logs/CliffStudyVideo/"+(frame++).ToString("D05")+".png",videoTexture.EncodeToPNG());
            }
            finally{camera.targetTexture=previous;RenderTexture.active=activeTarget;}
        }
        static TerraceJumpCheckCI() => EditorApplication.update+=Poll;
        public static void Check()
        {
            string scene="TerracePeak";var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-scenicScene")scene=args[i+1];
            bool record=Array.IndexOf(args,"-recordStudy")>=0;
            SessionState.SetBool(Armed+".Record",record);
            SessionState.SetBool(Armed+".Input",Array.IndexOf(args,"-inputStudy")>=0);
            if(record)
            {
                Directory.CreateDirectory("Logs/CliffStudyVideo");
                foreach(var file in Directory.GetFiles("Logs/CliffStudyVideo","*.png"))File.Delete(file);
            }
            SessionState.SetInt(Armed+".Count",scene=="TerraceCliffStudy"?7:GoatDescent.Levels.DescentCourseLayout.JumpCount);
            EditorSceneManager.OpenScene("Assets/GoatDescent/Scenes/"+scene+".unity");
            SessionState.SetBool(Armed,true);EditorApplication.EnterPlaymode();
        }
        private static void Poll()
        {
            if(!SessionState.GetBool(Armed,false)||!EditorApplication.isPlaying||Time.time<2)return;
            try
            {
                if(!course)
                {
                    course=UnityEngine.Object.FindFirstObjectByType<ScenicGoatCourse>();
                    goat=UnityEngine.Object.FindFirstObjectByType<GoatController>();
                    if(!course||!goat||!course.Descending || course.JumpTakeoffs.Length!=SessionState.GetInt(Armed+".Count",96))throw new Exception("Missing jump course");
                    life=goat.GetComponent<RespawnController>();body=goat.GetComponent<Rigidbody>();
                    goat.SetRemoteControl(true);course.enabled=false;
                    Time.timeScale=SessionState.GetBool(Armed+".Record",false)?1:2;
                    if(SessionState.GetBool(Armed+".Record",false))Time.captureFramerate=30;
                }
                var takeoff=course.JumpTakeoffs[index];var landing=course.JumpLandings[index];
                if(!active)
                {
                    heading=Vector3.ProjectOnPlane(landing-takeoff,Vector3.up).normalized;
                    life.SetKillPlane(takeoff.y-15);
                    if(index==0 || course.gameObject.scene.name!="TerraceCliffStudy")
                        life.Teleport(course.Points[index]-heading*.5f+Vector3.up*1.2f);
                    started=Time.time;settledAt=-1;active=true;released=airborne=charging=false;aligned=course.gameObject.scene.name!="TerraceCliffStudy";

                }
                var aim=course.Points[index+1];
                Vector3 error=Vector3.ProjectOnPlane(aim-body.position,Vector3.up);
                Vector3 velocity=Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up);
                if(!aligned)
                {
                    var approach=Vector3.ProjectOnPlane(course.Points[index]-body.position,Vector3.up);
                    goat.SetRemoteMove(Vector3.ClampMagnitude((approach*1.4f-velocity)*.7f,1));
                    Record();
                    if(life.IsDead||Time.time-started>12)throw new Exception("Study approach failed on "+index);
                    if(approach.magnitude<.55f&&velocity.magnitude<1.4f&&goat.Grounded)aligned=true;
                    return;
                }
                goat.SetRemoteMove(released?Vector3.ClampMagnitude((error*.8f-velocity)*.7f,1):heading);
                float edge=Vector3.Dot(takeoff-body.position,heading);
                bool hard=Vector3.ProjectOnPlane(takeoff-landing,Vector3.up).magnitude>3.4f;
                bool input=SessionState.GetBool(Armed+".Input",false);
                if(input)
                {
                    var jump=goat.GetComponent<GoatJumpController>();
                    if(!charging&&!released&&goat.Grounded&&edge<(hard?4.2f:1.1f))
                    {
                        jump.NetworkSpaceDown();charging=true;
                        if(!hard){jump.NetworkSpaceUp();released=true;}
                    }
                    if(hard&&charging&&!released&&edge<1.4f){jump.NetworkSpaceUp();released=true;}
                }
                else if(!released && goat.Grounded && edge<1f)
                {
                    var tuning=GoatMechanics.For(goat);
                    goat.Jump(hard?tuning.jumpVelocity+tuning.chargedVerticalBonus:tuning.jumpVelocity,
                        hard?tuning.chargedForwardBoost:0f);
                    released=true;
                }
                Record();
                if(released&&!goat.Grounded)airborne=true;
                if(life.IsDead||Time.time-started>12||body.position.y<landing.y-6)
                    throw new Exception($"Jump {index} failed edge={edge:F2} position={body.position} released={released} airborne={airborne} trick={goat.GetComponent<GoatJumpController>().CurrentTrick} charge={goat.GetComponent<GoatJumpController>().Charge01}");
                if(!airborne||!goat.Grounded||Vector3.Distance(Vector3.ProjectOnPlane(body.position-aim,Vector3.up),Vector3.zero)>2f)
                {settledAt=-1;return;}
                if(course.gameObject.scene.name=="TerraceCliffStudy")
                {
                    goat.SetRemoteMove(Vector3.ClampMagnitude(-velocity*.7f,1));
                    if(settledAt<0)settledAt=Time.time;
                    if(Time.time-settledAt<.45f)return;
                }
                if(goat.GetComponent<GoatHealth>().Current < 99f)throw new Exception("Landing damage on jump "+index);
                Debug.Log($"TERRACE_JUMP_OK index={index} drop={takeoff.y-landing.y:F2} seconds={Time.time-started:F2}");
                if(course.gameObject.scene.name=="TerraceCliffStudy" )
                    ScenicWorldAuthoring.Capture(Camera.main,"CliffStudy-gameplay-"+(index+1));
                index++;active=false;
                if(index<course.JumpTakeoffs.Length)return;
                if(course.gameObject.scene.name=="TerraceCliffStudy")
                {
                    var atmosphere=course.GetComponent<CliffStudyAtmosphere>();
                    if(!atmosphere||!atmosphere.WindPlaying||atmosphere.LandingPuffs<7||atmosphere.Footsteps<1)throw new Exception("Study atmosphere failed");
                    Debug.Log($"CLIFF_ATMOSPHERE_OK puffs={atmosphere.LandingPuffs} footsteps={atmosphere.Footsteps} wind={atmosphere.WindPlaying}");
                }
                Debug.Log("TERRACE_ALL_JUMPS_OK count="+index);
                SessionState.SetBool(Armed,false);EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);SessionState.SetBool(Armed,false);EditorApplication.Exit(1);}
        }
    }
}
