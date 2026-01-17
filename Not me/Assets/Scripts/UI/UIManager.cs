using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public Slider itemSlider;
    public Slider progressSlider;
    public TextMeshProUGUI itemText;

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
        float finishLineX = 500f; //최종점 : 500f 거리
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
        }
    }
}
