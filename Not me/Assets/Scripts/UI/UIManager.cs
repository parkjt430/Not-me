using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public Slider itemSlider;
    public Slider progressSlider;
    public TextMeshProUGUI itemText;
    public GameObject glitchEffectPanel; // 글리치 효과 패널 (UI에 추가 필요)
    public float finishLineX = 500f; //최종점 : 500f 거리

    private PlayerController targetPlayer;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterPlayer(PlayerController player)
    {
        // Only register local player
        targetPlayer = player;
    }

    void Update()
    {
        if (targetPlayer == null) return;

        // 아이템 게이지 (NetworkVariable 접근)
        itemSlider.value = targetPlayer.itemGauge.Value;

        //보유 아이템 텍스트 표시(임시)
        UpdateItemText();

        // 진행도 게이지
        progressSlider.value = targetPlayer.transform.position.x / finishLineX;
    }

    void UpdateItemText()
    {
        switch (targetPlayer.itemObtained.Value)
        {
            case 0: itemText.text = "EMPTY"; break;
            case 1: itemText.text = "Taser Drone"; break;
            case 2: itemText.text = "Adrenaline"; break;
            case 3: itemText.text = "GravityShackle"; break;
            case 4: itemText.text = "NeuroVirus"; break;
            case 5: itemText.text = "EMPEmitter"; break;
            case 6: itemText.text = "GlitchScreen"; break;
            case 7: itemText.text = "Firewall"; break;
            case 8: itemText.text = "Chemical"; break;
        }
    }

    public void EnableGlitchEffect(bool enable)
    {
        if (glitchEffectPanel != null)
        {
            glitchEffectPanel.SetActive(enable);
        }
    }
}
