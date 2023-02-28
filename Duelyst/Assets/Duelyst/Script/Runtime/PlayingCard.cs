using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events; 
using EnumTypes;
using Unity.VisualScripting;

public class PlayingCard : MonoBehaviour
{
    public UnityEvent deathEvent = new UnityEvent();

    private int _power;
    public int Power { get { return _power; } private set { SetPower(value); } }
    private int _health;
    public int Health { get { return _health; } private set { SetHealth(value); } }
    public PlayerType Owner { get; private set; }

    [field: SerializeField]
    public int MoveChance { get; private set; }
    [field: SerializeField]
    public int AttackChance { get; private set; }
    [field: SerializeField]
    public bool IsGeneral { get; protected set; } = false;

    private Card cardData;
    private GameObject cardSprite;
    private Animator cardAnimator;
    private TMP_Text powerText;
    private TMP_Text healthText;

    private PlayingCardDirection defaultDirection;

    private Image image;
    private Material defaultMat;
    private Material allyOutline;
    private Material enemyOutline;

    private void Awake()
    {
        cardSprite = gameObject.FindChildGO(Functions.NAME__PLAYING_CARD__CARD_SPRITE);
        cardAnimator = cardSprite.GetComponent<Animator>();
        powerText = gameObject.FindChildGO(Functions.NAME__PLAYING_CARD__POWER_TEXT).GetComponent<TMP_Text>();
        healthText = gameObject.FindChildGO(Functions.NAME__PLAYING_CARD__HEALTH_TEXT).GetComponent<TMP_Text>();

        image = cardSprite.GetComponent<Image>();
        defaultMat = image.material;
        allyOutline = Functions.ALLY_OUTLINE;
        enemyOutline = Functions.ENEMY_OUTLINE;
    }

    public virtual void SetUp(Card card, PlayerType owner, int row, bool isRush)
    {
        cardData = card;

        cardAnimator.runtimeAnimatorController = cardData.Anim;
        cardAnimator.SetBool("onField", true);

        Power = cardData.Power;
        Health = cardData.Health;

        Owner = owner;
        defaultDirection = Owner == PlayerType.ME ? GameManager.Instance.MyDefaultDirection : GameManager.Instance.OpponentDefaultDirection;
        ChangeDirection(0, 0);

        MoveChance = isRush ? 1 : 0;
        AttackChance = isRush ? 1 : 0;

        GameManager.Instance.turnEndEvent.AddListener(Refresh);

        if (!isRush)
        {
            PaintGray();
        }

        SetLayer(row);
    }

    public IEnumerator Move(Tile sourceTile, Tile destTile)//, int destRow, int sourceCol, int destCol)
    {
        --MoveChance;

        SetLayer(destTile.RowIndex);

        int frame = 60;

        Vector3 destPos = destTile.transform.GetComponent<RectTransform>().position;
        Vector3 sourcePos = transform.position;
        float timer = 1f;
        float term = (float)1f / frame;
        float speed = 1.5f;

        //방향전환
        ChangeDirection(sourceTile.ColumnIndex, destTile.ColumnIndex);

        while (timer >= 0)
        {
            transform.position = Vector3.Lerp(sourcePos, destPos, 1 - timer);

            yield return new WaitForSeconds(term);
            timer -= term * speed;
        }

        destTile.OnPlaceEffect();

        //기본방향으로 전환
        ChangeDirection(0, 0);
    }

    public IEnumerator Battle(PlayingCard target, int sourceCol, int destCol)
    {
        if (target == null)
            yield break;

        --MoveChance;
        --AttackChance;

        //방향전환
        ChangeDirection(sourceCol, destCol);

        cardAnimator.SetTrigger("onAttack");
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(cardAnimator.GetCurrentAnimatorStateInfo(0).length);

        //기본방향으로 전환
        ChangeDirection(0, 0);

        target.cardAnimator.SetTrigger("isDamaged");
        yield return new WaitForEndOfFrame();
        target.Health -= Power;
        yield return new WaitForSeconds(target.cardAnimator.GetCurrentAnimatorStateInfo(0).length);

        //방향전환
        target.ChangeDirection(destCol, sourceCol);

        target.cardAnimator.SetTrigger("onAttack");
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(target.cardAnimator.GetCurrentAnimatorStateInfo(0).length);

        //기본방향으로 전환
        target.ChangeDirection(0, 0);

        cardAnimator.SetTrigger("isDamaged");
        yield return new WaitForEndOfFrame();
        Health -= target.Power;
        yield return new WaitForSeconds(cardAnimator.GetCurrentAnimatorStateInfo(0).length);

        //die check
        if (Health <= 0)
        {
            cardAnimator.SetTrigger("isDead");
            if (deathEvent != null)
                deathEvent.Invoke();

            yield return new WaitForEndOfFrame();
        }
        if (target.Health <= 0)
        {
            target.cardAnimator.SetTrigger("isDead");
            if (target.deathEvent != null)
                target.deathEvent.Invoke();

            yield return new WaitForEndOfFrame();
        }

        if (Health <= 0 && target.Health <= 0)
        {
            yield return new WaitForSeconds(Mathf.Max(cardAnimator.GetCurrentAnimatorStateInfo(0).length, target.cardAnimator.GetCurrentAnimatorStateInfo(0).length));

            PlayingCardPoolingManager.Instance.Inactive(this);
            PlayingCardPoolingManager.Instance.Inactive(target);
        }
        else if (Health <= 0)
        {
            yield return new WaitForSeconds(cardAnimator.GetCurrentAnimatorStateInfo(0).length);

            PlayingCardPoolingManager.Instance.Inactive(this);
        }
        else if (target.Health <= 0)
        {
            yield return new WaitForSeconds(target.cardAnimator.GetCurrentAnimatorStateInfo(0).length);

            PlayingCardPoolingManager.Instance.Inactive(target);
        }

        if (AttackChance <= 0)
            PaintGray();
    }

    public void PaintGray()
    {
        image.color = Color.gray;
    }

    public void PaintDefault()
    {
        image.color = Color.white;
    }

    public void ShowOutline()
    {
        image.material = Owner == PlayerType.ME ? allyOutline : enemyOutline;
    }

    public void HideOutline()
    {
        image.material = defaultMat;
    }

    private void Refresh()
    {
        PaintDefault();
        if (GameManager.Instance.CurrentTurnPlayer == Owner)
        {
            MoveChance = 1;
            AttackChance = 1;
        }
    }

    private void SetLayer(int layerNum)
    {
        transform.SetParent(GameManager.Instance.Layers[layerNum]);
    }

    private void SetPower(int currentPower)
    {
        _power = currentPower;
        powerText.SetTMPText(Power);
    }

    private void SetHealth(int currentHealth)
    {
        _health = currentHealth;
        healthText.SetTMPText(Health);
    }

    private void ChangeDirection(int sourceCol, int destCol)
    {
        if (destCol - sourceCol < 0)
        {
            cardSprite.transform.rotation = Quaternion.Euler(0, (int)PlayingCardDirection.Left, 0);
        }
        else if (destCol - sourceCol > 0)
        {
            cardSprite.transform.rotation = Quaternion.Euler(0, (int)PlayingCardDirection.Right, 0);
        }
        else
        {
            cardSprite.transform.rotation = Quaternion.Euler(0, (int)defaultDirection, 0);
        }
    }
}