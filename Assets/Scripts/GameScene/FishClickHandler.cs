using UnityEngine;
using UnityEngine.EventSystems;

public class FishClickHandler : MonoBehaviour, IPointerClickHandler
{
    private FishCollectionManager collectionManager;
    private FishController fishController;
    private string boardName;

    public void Initialize(FishCollectionManager manager, FishController fish, string board)
    {
        collectionManager = manager;
        fishController = fish;
        boardName = board;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        collectionManager?.OnFishClicked(fishController, boardName);
    }
}