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
    public Card Data { get; private set; }

    private GameObject cardSprite;
    private Animator cardAnimator;
    private TMP_Text powerText;
    private TMP_Text healthText;

    private PlayingCardDirection defaultDirection;

    private Image image;
    private Material defaultMat;
    private Material allyOutline;
    private Material enemyOutline;

    private bool isAttackEnd;
    private bool isHitEnd;
    private bool isDeathEnd;

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

    public void SetUp(Card card, PlayerType owner)
    {
        isAttackEnd = false;
        isHitEnd = false;
        isDeathEnd = false;

        Data = card;
        gameObject.name = Data.Name;
        cardAnimator.runtimeAnimatorController = Data.Anim;
        Power = Data.Power;
        Health = Data.Health;
        Owner = owner;

        defaultDirection = Owner == PlayerType.ME ? GameManager.Instance.MyDefaultDirection : GameManager.Instance.OpponentDefaultDirection;
        ChangeDirection(0, 0);

        GameManager.Instance.turnEndEvent.AddListener(Refresh);

        cardAnimator.SetBool("onField", true);

        if (Data.IsRush || Data.Type == CardType.GENERAL)
        {
            MoveChance = 1;
            AttackChance = 1;
            PaintDefault();
        }
        else
        {
            MoveChance = 0;
            AttackChance = 0;
            PaintGray();
        }
    }

    public IEnumerator Move(Tile sourceTile, Tile destTile)
    {
        --MoveChance;

        SetLayer(destTile.Row);

        int frame = 100;

        Vector3 destPos = destTile.transform.GetComponent<RectTransform>().position;
        Vector3 sourcePos = transform.position;
        float timer = 1f;
        float term = 1f / frame;
        float speed = 1.25f;

        //방향전환
        cardAnimator.SetBool("isMoving", true);
        ChangeDirection(sourceTile.Column, destTile.Column);

        while (timer >= 0)
        {
            transform.position = Vector3.Lerp(sourcePos, destPos, 1 - timer);

            yield return new WaitForSeconds(term);
            timer -= term * speed;
        }

        //기본방향으로 전환
        ChangeDirection(0, 0);
        cardAnimator.SetBool("isMoving", false);

        destTile.OnPlaceEffect();
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
        yield return new WaitUntil(() => isAttackEnd);
        isAttackEnd = false;

        //기본방향으로 전환
        ChangeDirection(0, 0);

        target.cardAnimator.SetTrigger("isDamaged");
        target.Health -= Power;
        yield return new WaitUntil(() => target.isHitEnd);
        target.isHitEnd = false;

        //방향전환
        target.ChangeDirection(destCol, sourceCol);

        target.cardAnimator.SetTrigger("onAttack");
        yield return new WaitUntil(() => target.isAttackEnd);
        target.isAttackEnd = false;

        //기본방향으로 전환
        target.ChangeDirection(0, 0);

        cardAnimator.SetTrigger("isDamaged");
        Health -= target.Power;
        yield return new WaitUntil(() => isHitEnd);
        isHitEnd = false;

        //die check
        if (Health <= 0)
        {
            cardAnimator.SetTrigger("isDead");
            if (deathEvent != null)
                deathEvent.Invoke();
        }
        if (target.Health <= 0)
        {
            target.cardAnimator.SetTrigger("isDead");
            if (target.deathEvent != null)
                target.deathEvent.Invoke();
        }

        if (Health <= 0 && target.Health <= 0)
        {
            yield return new WaitUntil(() => isDeathEnd && target.isDeathEnd);
            isDeathEnd = false;
            target.isDeathEnd = false;

            PlayingCardPoolingManager.Instance.InactiveCard(this);
            PlayingCardPoolingManager.Instance.InactiveCard(target);
        }
        else if (Health <= 0)
        {
            yield return new WaitUntil(() => isDeathEnd);
            isDeathEnd = false;

            PlayingCardPoolingManager.Instance.InactiveCard(this);
        }
        else if (target.Health <= 0)
        {
            yield return new WaitUntil(() => target.isDeathEnd);
            target.isDeathEnd = false;

            PlayingCardPoolingManager.Instance.InactiveCard(target);
        }

        if (AttackChance <= 0)
            PaintGray();
    }

    public void PaintGray()
    {
        if (Data.Type == CardType.MINION || Data.Type == CardType.GENERAL)
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
    public void SetLayer(int layerNum)
    {
        transform.SetParent(GameManager.Instance.Layers[layerNum]);
    }

    private void Refresh()
    {
        if (GameManager.Instance.CurrentTurnPlayer == Owner)
        {
            MoveChance = 1;
            AttackChance = 1;
            PaintDefault();
        }
    }


    private void SetPower(int currentPower)
    {
        _power = currentPower;
        powerText.SetTMPText(Power);
    }

    protected virtual void SetHealth(int currentHealth)
    {
        _health = currentHealth;
        if (_health < 0)
            _health = 0;
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

    public void AttackAnimEndEvent()
    {
        isAttackEnd = true;
    }

    public void HitAnimEndEvent()
    {
        isHitEnd = true;
    }

    public void DeathAnimEndEvent()
    {
        isDeathEnd = true;
    }
}