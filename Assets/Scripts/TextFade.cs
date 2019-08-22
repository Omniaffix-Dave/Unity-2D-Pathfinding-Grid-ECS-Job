using UnityEngine;
using UnityEngine.UI;

namespace UnityTemplateProjects
{
    [RequireComponent(typeof(Text))]
    public class TextFade : UnityEngine.MonoBehaviour
    {
        public float speed = 0.25f;
        public float wait = 3f;
        [Space(10)]
        public bool fadeIn = false;
        public bool fadeOut = false;
        [Space(5)]
        public bool destroyAfterwards = false;
        
        private Color initialColor;
        private Text text;

        private CurrentStage currentStage;
        private enum CurrentStage
        {
            fadeIn,
            wait,
            fadeOut
        }

        private float waitedAlready = 0;

        private void OnEnable()
        {
            text = GetComponent<Text>();
            initialColor = text.color;

            if (fadeOut) {    currentStage = CurrentStage.fadeOut;    }
            if (fadeIn)
            {
                var temp = text.color;
                temp.a = 0;
                text.color = temp;
                
                currentStage = CurrentStage.fadeIn;
            }
        }

        private void Update()
        {
            var currentColor = text.color;

            if (currentStage == CurrentStage.wait)
            {
                waitedAlready += Time.deltaTime;
                if (waitedAlready >= wait)
                {
                    currentStage = CurrentStage.fadeOut;
                }
                else
                {
                    return;
                }
            }
            
            if (fadeIn && currentStage == CurrentStage.fadeIn)
            {
                var targetAlpha = initialColor.a;
                currentColor.a += speed * Time.deltaTime;
                text.color = currentColor;

                if (currentColor.a >= initialColor.a)
                {
                    if (fadeOut)
                    {
                        if(wait <= 0) {    currentStage = CurrentStage.fadeOut;    }
                        else         {    currentStage = CurrentStage.wait;    }
                        
                        return;
                    }
                    else
                    {
                        Final();
                    }
                }
            }

            if (fadeOut && currentStage == CurrentStage.fadeOut)
            {
                if (currentColor.a > 0)
                {
                    currentColor.a -= speed * Time.deltaTime;
                    text.color = currentColor;
                }
                else
                {
                    Final();
                }
            }
        }

        private void Final()
        {
            waitedAlready = 0;
            if (destroyAfterwards)
            {
                Destroy(this.gameObject);
            }
            else
            {
                text.color = initialColor;
                this.gameObject.SetActive(false);
            }
        }

        public void Hide()
        {
            waitedAlready = wait;
        }
    }
}