using UnityEngine;

namespace GoatDescent
{
    public sealed class SlopeRun : MonoBehaviour
    {
        public static SlopeRun Instance { get; private set; }
        public bool Finished { get; private set; }
        private GoatController goat;
        private float elapsed,noticeUntil;
        private string notice;
        private bool help;
        private GUIStyle title,copy;
        private void Awake()=>Instance=this;
        private void OnDestroy(){if(Instance==this)Instance=null;}
        public void Notify(string message){notice=message;noticeUntil=Time.unscaledTime+3f;}
        public void ResetRun(){elapsed=0f;Finished=false;Notify("Снова на вершине. Выбирай свой путь.");}
        private void Update()
        {
            if(!goat)goat=FindFirstObjectByType<GoatController>();
            if(!goat)return;
            if(!Finished)elapsed+=Time.unscaledDeltaTime;
            if(Input.GetKeyDown(KeyCode.F1))help=!help;
            if(!Finished && goat.transform.position.z>310f && goat.transform.position.y<14f && goat.Grounded)
            {Finished=true;Notify("ДОБРАЛСЯ ДО ДОЛИНЫ! R — ещё один спуск.");goat.GetComponent<GoatSpectacle>()?.Finish();}
        }
        private string Region(float z)
        {
            if(z< -36f)return "ЗАТОРМОЗИ ПЕРЕД ПОЛКОЙ";
            if(z< -6f)return "СКАЛЬНЫЕ ОТСКОКИ";
            if(z<24f)return "ОБМАНЧИВАЯ ПОЛКА";
            if(z<78f)return "РАЗГОН И ОБРЫВ";
            if(z<150f)return "РАЗВИЛКА НА ГРЕБНЕ";
            if(z<215f)return "ПОСЛЕДНИЙ СБРОС";
            return "НИЖНЯЯ КОТЛОВИНА";
        }
        private void OnGUI()
        {
            if(!goat)return;
            title??=new GUIStyle(GUI.skin.label){fontSize=21,fontStyle=FontStyle.Bold,normal={textColor=new Color(.98f,.85f,.59f)}};
            copy??=new GUIStyle(GUI.skin.label){fontSize=14,normal={textColor=Color.white},wordWrap=true};
            float scale=Mathf.Min(Screen.width/1050f,Screen.height/650f);
            var matrix=GUI.matrix;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float height=Screen.height/scale,width=Screen.width/scale;
            GUI.color=new Color(.04f,.08f,.12f,.82f);GUI.DrawTexture(new Rect(20,20,410,help?168:91),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(35,30,390,30),Finished?"ГОРА ПРОЙДЕНА":Region(goat.transform.position.z),title);
            GUI.Label(new Rect(35,67,385,28),$"Скорость {goat.Velocity.magnitude*3.6f:0} км/ч  •  Высота {Mathf.Max(0,goat.transform.position.y):0} м  •  {(int)elapsed/60:00}:{(int)elapsed%60:00}",copy);
            if(help)GUI.Label(new Rect(35,103,380,75),"WASD — рулить • Ctrl — зацеп копытами\nF — суперкопыта для спасения у скалы\nSpace — прыжок • E — прицельный отскок",copy);
            GUI.Label(new Rect(23,height-33,width-46,25),"Ctrl — зацеп   •   F — суперкопыта   •   E — отскок   •   R — вершина   •   F1 — помощь",copy);
            var grip=goat.GetComponent<GoatGripController>();
            var wall=goat.GetComponent<GoatWallJumpController>();
            if(grip)
            {
                float right=width-270f;
                GUI.color=new Color(.04f,.08f,.12f,.82f);GUI.DrawTexture(new Rect(right,20,250,216),Texture2D.whiteTexture);GUI.color=Color.white;
                GUI.Label(new Rect(right+14,30,225,25),grip.Exhausted?"КОПЫТА СОРВАЛИСЬ":grip.IsGripping?"ДЕРЖИМСЯ КОПЫТАМИ":"ЗАПАС ЗАЦЕПА",copy);
                GUI.color=new Color(.2f,.27f,.31f);GUI.DrawTexture(new Rect(right+14,62,220,9),Texture2D.whiteTexture);
                GUI.color=grip.Exhausted?new Color(.92f,.35f,.25f):new Color(.9f,.73f,.38f);GUI.DrawTexture(new Rect(right+14,62,220*grip.Strength,9),Texture2D.whiteTexture);GUI.color=Color.white;
                string hint=wall&&wall.IsAiming?$"Мышь — направление\nОтпусти E • {wall.AimTimeLeft:0.0} сек.":wall&&wall.WallNearby?$"Зажми E для отскока\nОсталось отскоков: {wall.JumpsLeft}":"Отпусти Ctrl на полке,\nчтобы восстановить зацеп.";
                GUI.Label(new Rect(right+14,84,225,59),hint,copy);
                var balance=goat.GetComponent<GoatSlopeBalance>();
                if(balance)
                {
                    GUI.Label(new Rect(right+14,147,225,22),$"БАЛАНС • копыта {balance.HoovesHolding}/4",copy);
                    GUI.color=new Color(.2f,.27f,.31f);GUI.DrawTexture(new Rect(right+14,175,220,9),Texture2D.whiteTexture);
                    GUI.color=Color.Lerp(new Color(.94f,.33f,.23f),new Color(.46f,.82f,.55f),balance.Balance01);
                    GUI.DrawTexture(new Rect(right+14,175,220*balance.Balance01,9),Texture2D.whiteTexture);
                    GUI.color=Color.white;
                }
                GUI.Label(new Rect(right+14,190,225,20),"СУПЕРКОПЫТА — F",copy);
                GUI.color=new Color(.2f,.27f,.31f);GUI.DrawTexture(new Rect(right+14,215,220,9),Texture2D.whiteTexture);
                GUI.color=new Color(.38f,.92f,.85f);GUI.DrawTexture(new Rect(right+14,215,220*grip.SuperCharge01,9),Texture2D.whiteTexture);
                GUI.color=Color.white;
            }
            if(Time.unscaledTime<noticeUntil)GUI.Label(new Rect(width*.5f-260,height-80,520,42),notice,copy);
            GUI.matrix=matrix;
        }
    }
}
