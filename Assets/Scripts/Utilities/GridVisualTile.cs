using UnityEngine;
using TMPro;

namespace Egglers
{
    /// <summary>
    /// Handles visual representation of a tile: plants, billboard, and pollution text.
    /// Listens to grid events and updates accordingly.
    /// Uses the unified GridSystem for all entity access.
    /// </summary>
    public class GridVisualTile : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] public GameManager gameManager;
        [SerializeField] public PlantBitManager plantBitManager;
        [SerializeField] public PollutionManager pollutionManager;

        [Header("Plant Objects")]
        public GameObject motherPlant;
        public GameObject bud;
        public GameObject root;
        public GameObject budInfected;
        public GameObject budInfectedLeaf;
        public GameObject budInfectedFruit;
        public GameObject grown;
        public GameObject grownFruit;
        public GameObject grownFruitMax;
        public GameObject grownLeafMax;

        [Header("Billboard / Pollution")]
        public TMP_Text tmp;
        [SerializeField] private PollutionParticles pollutionParticles;
        [SerializeField] private GameObject pollutionSourceVisual;
        [SerializeField] private GameObject pollutionSourceWaterVisual;
        [SerializeField] private GameObject pollutionVisual;
        [SerializeField] private Renderer pollutionRenderer;
        [SerializeField] private Renderer pollutionSourceWaterRenderer;
        [SerializeField] private Color pollutionBaseColor = Color.white;
        [SerializeField] private Color pollutionVoroniColor = Color.white;
        [SerializeField] private float colorIntensityMin = -7f;
        [SerializeField] private float colorIntensityMax = 7f;
        [SerializeField] private string baseColorProperty = "_BaseColor";
        [SerializeField] private string voroniColorProperty = "_voroniColor";

        private const float SpreadNormalization = 75f;
        private const float StrengthNormalization = 75f;
        private const float ResistanceNormalization = 75f;


        private MaterialPropertyBlock pollutionPropertyBlock;
        private MaterialPropertyBlock waterPropertyBlock;
        private int baseColorId;
        private int voroniColorId;

        [Header("Grid Info")]
        public Vector2Int coords;

        private bool isShowingHeartPreview = false;

        private void Awake()
        {
            baseColorId = Shader.PropertyToID(baseColorProperty);
            voroniColorId = Shader.PropertyToID(voroniColorProperty);
            pollutionRenderer = pollutionVisual.GetComponentInChildren<Renderer>();
            pollutionPropertyBlock = new MaterialPropertyBlock();
            pollutionSourceWaterRenderer = pollutionSourceWaterVisual.GetComponentInChildren<Renderer>();
            waterPropertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            GridEvents.OnPlantUpdated += HandlePlantUpdate;
            GridEvents.OnPollutionUpdated += HandlePollutionUpdate;
            GridEvents.OnPlantKilledByPollution += HandlePlantKilledByPollution;
            GridEvents.OnPollutionKilledByPlant += HandlePollutionKilledByPlant;
        }

        private void OnDisable()
        {
            GridEvents.OnPlantUpdated -= HandlePlantUpdate;
            GridEvents.OnPollutionUpdated -= HandlePollutionUpdate;
            GridEvents.OnPlantKilledByPollution -= HandlePlantKilledByPollution;
            GridEvents.OnPollutionKilledByPlant -= HandlePollutionKilledByPlant;
        }

        #region Grid Event Handlers

        private void HandlePlantUpdate(Vector2Int pos)
        {
            if (pos != coords) return;

            PlantBit bit = gameManager.gameGrid.GetEntity<PlantBit>(pos);
            UpdatePlantVisuals(bit);
        }

        private void HandlePollutionUpdate(Vector2Int pos)
        {
            if (pos != coords) return;

            PollutionTile tile = gameManager.gameGrid.GetEntity<PollutionTile>(coords);
            PollutionSource source = gameManager.gameGrid.GetEntity<PollutionSource>(coords);

            float level = 0f;
            if (tile != null) level = tile.GetTotalPollution();
            else if (source != null) level = source.GetTotalPollution();

            if (tmp != null)
                tmp.text = level.ToString("0.00");

            float spreadRate = 0f;
            float strength = 0f;
            float resistance = 0f;

            if (tile != null)
            {
                spreadRate = tile.pollutionSpreadRate;
                strength = tile.pollutionStrength;
                resistance = tile.pollutionResistance;
            }
            else if (source != null)
            {
                spreadRate = source.pollutionSpreadRate;
                strength = source.pollutionStrength;
                resistance = source.pollutionResistance;
            }

            float spreadIntensity = Mathf.Clamp01(spreadRate / SpreadNormalization);
            float strengthIntensity = Mathf.Clamp01(strength / StrengthNormalization);
            float resistanceIntensity = Mathf.Clamp01(resistance / ResistanceNormalization);

            UpdatePollutionMaterial(strengthIntensity, resistanceIntensity);
            UpdateWaterMaterial(strengthIntensity, resistanceIntensity);
            UpdatePollutionVisualState(tile != null || (source != null && source.IsActive), source);
        }
        private void HandlePlantKilledByPollution(Vector2Int pos)
        {
            if (pos != coords || pollutionParticles == null) return;
            pollutionParticles.PlayPlantDeath();
        }

        private void HandlePollutionKilledByPlant(Vector2Int pos)
        {
            if (pos != coords || pollutionParticles == null) return;
            pollutionParticles.PlayPollutionDeath();
        }


        #endregion

        private void UpdatePollutionVisualState(bool hasPollution, PollutionSource source)
        {
            bool hasSource = source != null;
            bool isActive = hasSource && source.IsActive;

            pollutionSourceVisual.SetActive(hasSource);
            pollutionSourceWaterVisual.SetActive(isActive);
            pollutionVisual.SetActive(hasPollution);
        }

        private void UpdatePollutionMaterial(float strengthIntensity, float resistanceIntensity)
        {
            if (pollutionRenderer == null || pollutionPropertyBlock == null)
            {
                return;
            }

            pollutionRenderer.GetPropertyBlock(pollutionPropertyBlock);

            Color baseColor = ApplyHdrIntensity(pollutionBaseColor, strengthIntensity);
            Color voroniColor = ApplyHdrIntensity(pollutionVoroniColor, resistanceIntensity);

            if (baseColorId != 0)
            {
                pollutionPropertyBlock.SetColor(baseColorId, baseColor);
            }
            if (voroniColorId != 0)
            {
                pollutionPropertyBlock.SetColor(voroniColorId, voroniColor);
            }

            pollutionRenderer.SetPropertyBlock(pollutionPropertyBlock);
        }

        private void UpdateWaterMaterial(float strengthIntensity, float resistanceIntensity)
        {
            if (pollutionSourceWaterRenderer == null || waterPropertyBlock == null)
            {
                return;
            }

            pollutionSourceWaterRenderer.GetPropertyBlock(waterPropertyBlock);

            Color baseColor = ApplyHdrIntensity(pollutionBaseColor, strengthIntensity);
            Color voroniColor = ApplyHdrIntensity(pollutionVoroniColor, resistanceIntensity);

            if (baseColorId != 0)
            {
                waterPropertyBlock.SetColor(baseColorId, baseColor);
            }
            if (voroniColorId != 0)
            {
                waterPropertyBlock.SetColor(voroniColorId, voroniColor);
            }

            pollutionSourceWaterRenderer.SetPropertyBlock(waterPropertyBlock);
        }

        private Color ApplyHdrIntensity(Color color, float normalizedValue)
        {
            float clampedNormalized = Mathf.Clamp01(normalizedValue);
            float hdrIntensity = Mathf.Lerp(colorIntensityMin, colorIntensityMax, clampedNormalized);
            float multiplier = Mathf.Pow(2f, hdrIntensity);
            Color adjusted = color * multiplier;
            adjusted.a = color.a;
            return adjusted;
        }

        public void RefreshActions()
        {
            TileActions tileActions = GetComponent<TileActions>();
            if (tileActions == null || tileActions.actions == null) return;

            Vector2Int pos = coords;

            bool hasPollution =
                PollutionManager.Instance.gameGrid.GetEntity<PollutionTile>(pos) != null ||
                PollutionManager.Instance.gameGrid.GetEntity<PollutionSource>(pos) != null;

            bool hasHeart =
                PlantBitManager.Instance.gameGrid.GetEntity<PlantBit>(pos)?.isHeart == true;

            foreach (var action in tileActions.actions)
            {
                switch (action.actionType)
                {
                    case TileActionType.PlaceHeart:
                        // Disable if pollution OR heart already exists
                        action.enabled = !hasPollution && !hasHeart;
                        break;

                    default:
                        action.enabled = true;
                        break;
                }
            }
        }

        #region Plant & Billboard Methods

        /// <summary>
        /// Sets only the specified plant active (by index 0-2), disables others.
        /// </summary>
        public void SetActivePlantByIndex(int index)
        {
            if (bud != null) bud.SetActive(index == 0);
            if (grown != null) grown.SetActive(index == 1);
            if (motherPlant != null) motherPlant.SetActive(index == 2);
            if (budInfected != null) budInfected.SetActive(index == 3);

            if (grownFruit != null) grownFruit.SetActive(index == 4);
            if (grownFruitMax != null) grownFruitMax.SetActive(index == 5);
            if (grownLeafMax != null) grownLeafMax.SetActive(index == 6);

            if (budInfectedLeaf != null) budInfectedLeaf.SetActive(index == 7);
            if (budInfectedFruit != null) budInfectedFruit.SetActive(index == 8);
        }



        /// <summary>
        /// Sets billboard or TMP text for the tile.
        /// </summary>
        public void SetBillboardText(string text)
        {
            if (tmp != null)
            {
                tmp.text = text;
            }
        }

        /// <summary>
        /// Updates plant visuals (sprites, scale, etc.) if needed.
        /// Customize to match your plant prefabs or animation logic.
        /// </summary>
        private void UpdatePlantVisuals(PlantBit bit)
        {
            if (bit == null)
            {
                SetActivePlantByIndex(-1);
                return;
            }

            // Heart always overrides visuals
            if (bit.isHeart)
            {
                SetActivePlantByIndex(2);
                return;
            }

            // Determine which component type is dominant
            int leaf = bit.leafCount + bit.graftedLeafCount;
            int root = bit.rootCount + bit.graftedRootCount;
            int fruit = bit.fruitCount + bit.graftedFruitCount;

            string dominant = "root";

            if (leaf >= root && leaf >= fruit) dominant = "leaf";
            else if (fruit >= leaf && fruit >= root) dominant = "fruit";

            // Handle infection phase first
            if (bit.phase == PlantBitPhase.FullyInfected)
            {
                switch (dominant)
                {
                    case "leaf":
                        SetActivePlantByIndex(7); // budInfectedLeaf
                        break;

                    case "fruit":
                        SetActivePlantByIndex(8); // budInfectedFruit
                        break;

                    case "root":
                        SetActivePlantByIndex(3); // budInfected
                        break;
                }
                return;
            }

            // Handle non-infected phases
            switch (bit.phase)
            {
                case PlantBitPhase.Bud:
                    // All buds use the same prefab for now
                    SetActivePlantByIndex(0);
                    break;

                case PlantBitPhase.Grown:
                    switch (dominant)
                    {
                        case "leaf":
                            SetActivePlantByIndex(6); // grownLeafMax
                            break;

                        case "fruit":
                            SetActivePlantByIndex(4); // grownFruit
                            break;

                        case "root":
                            SetActivePlantByIndex(1); // grown
                            break;
                    }
                    break;
            }
        }

        #endregion

        public void ShowHeartPreview()
        {
            if (isShowingHeartPreview) return;

            bool hasPollution =
                PollutionManager.Instance.gameGrid.GetEntity<PollutionTile>(coords) != null ||
                PollutionManager.Instance.gameGrid.GetEntity<PollutionSource>(coords) != null;

            if (hasPollution) return;

            motherPlant.SetActive(true);
            isShowingHeartPreview = true;
        }

        public void HideHeartPreview()
        {
            if (!isShowingHeartPreview) return;

            motherPlant.SetActive(false);
            isShowingHeartPreview = false;
        }

    }
}
