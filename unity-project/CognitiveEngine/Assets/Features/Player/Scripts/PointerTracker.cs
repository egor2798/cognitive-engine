using UnityEngine;
using UnityEngine.InputSystem;

public enum SensorControlMode
{
    TiltAngles,
    ImuDisplacement
}

public enum ImuAccelerationAxis
{
    AccX,
    AccY,
    AccZ,
    NegativeAccX,
    NegativeAccY,
    NegativeAccZ
}

public struct SensorImuData
{
    public float roll;
    public float pitch;
    public float yaw;

    // Acceleration in g, as sent by the bridge.
    public Vector3 accelerationG;

    // Gyroscope in degrees/second.
    public Vector3 gyroDegS;

    public bool hasAcceleration;
    public bool hasGyro;

    // Optional sequence id from Python bridge. Used to ignore repeated UDP packets
    // when UDP is sent faster than BLE sensor updates.
    public int sequenceId;
    public bool hasSequence;

    public Vector2 Angles2D => new Vector2(roll, pitch);
}

public class PointerTracker : MonoBehaviour
{
    [SerializeField] private PointerInputSource inputSource = PointerInputSource.Mouse;

    [Header("Sensor Mode")]
    [SerializeField] private SensorControlMode sensorControlMode = SensorControlMode.TiltAngles;

    [Header("Sensor Correction")]
    [Tooltip("Если включено, первый пакет с датчика принимается как нейтральное положение.")]
    [SerializeField] private bool autoCalibrateOnFirstData = true;

    [Tooltip("Нажать C во время Play Mode, чтобы принять текущее положение датчика за центр.")]
    [SerializeField] private bool calibrateByCKey = true;

    [Tooltip("Нейтральное положение датчика. Можно задать вручную или через CalibrateSensorCenter().")]
    [SerializeField] private Vector2 sensorCenter = Vector2.zero;

    [Tooltip("Сколько градусов наклона нужно для прохода от центра до края рабочей области. Меньше = чувствительнее.")]
    [SerializeField] private Vector2 sensorRange = new Vector2(20f, 20f);

    [Tooltip("Мёртвая зона в градусах. Убирает мелкое дрожание около центра.")]
    [SerializeField] private float deadZoneDegrees = 0.6f;

    [Tooltip("Сила сглаживания. Чем больше значение, тем быстрее курсор догоняет данные датчика. Рекомендуется 10-18.")]
    [Range(1f, 40f)]
    [SerializeField] private float followSpeed = 14f;

    [Tooltip("Дополнительное предсказание движения между редкими пакетами. 0 = выключено, 0.03-0.08 = мягко сглаживает рваность.")]
    [Range(0f, 0.15f)]
    [SerializeField] private float predictionTime = 0.04f;

    [Tooltip("Ограничение скорости визуального курсора в world units/second. 0 = без ограничения.")]
    [SerializeField] private float maxVisualSpeed = 0f;

    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;

    [Header("IMU Displacement Mode")]
    [Tooltip("Масштаб смещения по ускорению. Больше = курсор сильнее реагирует на перемещение датчика.")]
    [SerializeField] private float imuPositionGain = 1.2f;

    [Tooltip("Мёртвая зона ускорения в g. Убирает мелкое дрожание и шум акселерометра.")]
    [SerializeField] private float accelerationDeadZoneG = 0.035f;

    [Tooltip("Затухание скорости в режиме смещения. Больше = меньше дрейф.")]
    [Range(0f, 20f)]
    [SerializeField] private float velocityDamping = 7f;

    [Tooltip("Если скорость меньше этого порога и ускорение около нуля — скорость обнуляется.")]
    [SerializeField] private float zeroVelocityThreshold = 0.02f;

    [Tooltip("Максимальная скорость виртуального смещения в world units/second.")]
    [SerializeField] private float maxImuVelocity = 3f;

    [Tooltip("Стабильный режим внутри ImuDisplacement: ускорение управляет скоростью курсора, а не абсолютной позицией. Меньше дрейфа.")]
    [SerializeField] private bool stableImuMotion = true;

    [Tooltip("Как быстро скорость курсора следует за ускорением IMU. Больше = резче, меньше = плавнее.")]
    [Range(1f, 40f)]
    [SerializeField] private float imuVelocityResponse = 12f;

    [Tooltip("Как быстро курсор тормозит, когда датчик перестал двигаться. Больше = быстрее останавливается.")]
    [Range(0f, 40f)]
    [SerializeField] private float imuStopDamping = 18f;

    [Tooltip("Если включено, при почти нулевом ускорении скорость постепенно гасится до нуля.")]
    [SerializeField] private bool stopWhenAccelerationIsSmall = true;

    [Tooltip("Порог, ниже которого ускорение считается отсутствующим для торможения курсора.")]
    [SerializeField] private float stopAccelerationThresholdG = 0.006f;

    [Tooltip("Сколько секунд после короткого импульса ускорения курсор продолжает плавно двигаться, прежде чем начать активно тормозить. Помогает при движении датчика по столу: во время равномерного движения ускорение почти нулевое.")]
    [SerializeField] private float imuCoastTime = 0.22f;

    [Tooltip("Усиление малых импульсов ускорения. Если датчик иногда не реагирует на движение — увеличить до 2-4.")]
    [SerializeField] private float imuSmallSignalBoost = 2.5f;

    [Tooltip("Если движение в основном по X, подавлять паразитное Y. Полезно, когда датчик ведут по столу.")]
    [SerializeField] private bool suppressCrossAxisNoise = true;

    [Tooltip("Во сколько раз X должен быть больше Y, чтобы Y считался паразитным шумом и подавлялся.")]
    [SerializeField] private float crossAxisDominanceRatio = 1.8f;

    [Tooltip("Максимальное смещение от центра рабочей области в world units.")]
    [SerializeField] private Vector2 maxImuOffset = new Vector2(4f, 3f);

    [Tooltip("Использовать roll/pitch для вычитания гравитации из акселерометра. Экспериментально.")]
    [SerializeField] private bool removeGravityUsingAngles = true;

    [Tooltip("Если акселерометр не приходит или приходит нулями, временно использовать старый режим наклона как fallback.")]
    [SerializeField] private bool fallbackToTiltIfNoUsableAcceleration = true;

    [Tooltip("Во сколько раз переводить ускорение из g в рабочее смещение. Если IMU почти не двигает курсор — увеличивать.")]
    [SerializeField] private float accelerationToWorldScale = 9.81f;

    [Tooltip("Вычитать остаточное ускорение, измеренное во время калибровки. Помогает против дрейфа.")]
    [SerializeField] private bool subtractAccelerationBias = true;

    [Tooltip("Считать acc=0,0,0 как отсутствие акселерометра, а не как реальное нулевое ускорение.")]
    [SerializeField] private bool treatZeroAccelerationAsMissing = true;

    [Header("IMU Axis Stabilization")]
    [Tooltip("Какая ось ускорения управляет экранной осью X в режиме ImuDisplacement.")]
    [SerializeField] private ImuAccelerationAxis imuScreenXAxis = ImuAccelerationAxis.AccX;

    [Tooltip("Какая ось ускорения управляет экранной осью Y в режиме ImuDisplacement.")]
    [SerializeField] private ImuAccelerationAxis imuScreenYAxis = ImuAccelerationAxis.AccY;

    [Tooltip("Множитель чувствительности IMU по горизонтали.")]
    [SerializeField] private float imuXScale = 1f;

    [Tooltip("Множитель чувствительности IMU по вертикали. Если курсор сильно прыгает вверх/вниз, уменьшить до 0.05-0.2.")]
    [SerializeField] private float imuYScale = 0.15f;

    [Tooltip("Полностью заблокировать вертикальную ось в режиме ImuDisplacement. Полезно для теста движения датчика по столу.")]
    [SerializeField] private bool lockImuYAxis = false;

    [Tooltip("Если вертикальная ось заблокирована, возвращать Y в центр каждый кадр.")]
    [SerializeField] private bool keepLockedYAtCenter = true;

    [Tooltip("Дополнительная мёртвая зона только для вертикального IMU-ускорения. Помогает убрать скачки вверх/вниз.")]
    [SerializeField] private float imuYExtraDeadZoneG = 0.02f;

    [Header("IMU Rotation Suppression")]
    [Tooltip("Если датчик заметно поворачивается, временно подавлять ускорение. Это не даёт повороту корпуса датчика восприниматься как перемещение.")]
    [SerializeField] private bool ignoreAccelerationWhileRotating = true;

    [Tooltip("Порог угловой скорости, выше которого ускорение считается ненадёжным, deg/s.")]
    [SerializeField] private float rotationGyroThresholdDegS = 20f;

    [Tooltip("Во сколько раз оставлять ускорение при повороте. 0 = полностью игнорировать, 0.1 = оставить 10%.")]
    [Range(0f, 1f)]
    [SerializeField] private float rotationAccelerationSuppression = 0.1f;

    [Tooltip("Печатать в Console, когда ускорение подавляется из-за поворота датчика.")]
    [SerializeField] private bool logRotationSuppression = false;

    [Header("IMU Debug Console")]
    [Tooltip("Печатать в Console текущую скорость/ускорение/позицию в режиме ImuDisplacement.")]
    [SerializeField] private bool logImuVelocityToConsole = false;

    [Tooltip("Как часто печатать скорость в Console, секунд. Не ставь слишком мало, иначе Console будет спамить.")]
    [SerializeField] private float imuVelocityLogInterval = 0.25f;

    [Tooltip("Печатать только если скорость или ускорение заметные. Убирает лишние строки, когда датчик лежит спокойно.")]
    [SerializeField] private bool logOnlyWhenImuMoving = false;

    [Tooltip("Минимальная скорость для вывода в Console, world units/sec.")]
    [SerializeField] private float imuVelocityLogMinSpeed = 0.02f;

    [Tooltip("Минимальное ускорение для вывода в Console, g после фильтрации.")]
    [SerializeField] private float imuVelocityLogMinAccelerationG = 0.002f;


    [Header("IMU Bounds Handling")]
    [Tooltip("Не давать скрытому imuOffset уходить за видимую рабочую область. Скорость при этом не обнуляется, если выключен Stop Imu Velocity At Bounds.")]
    [SerializeField] private bool clampImuOffsetToWorldBounds = true;

    [Tooltip("Обнулять компонент скорости при упоре в край. Для IMU лучше держать false: координата ограничивается, а скорость не ломается.")]
    [SerializeField] private bool stopImuVelocityAtBounds = false;

    [Tooltip("Небольшой отступ от границы для определения упора в край.")]
    [SerializeField] private float imuBoundsEpsilon = 0.001f;


    [Header("IMU Direction Change Handling")]
    [Tooltip("Если ускорение направлено против текущей скорости, быстро гасить старую скорость. Убирает задержку при смене направления.")]
    [SerializeField] private bool fastBrakeOnDirectionChange = true;

    [Tooltip("Во сколько раз оставить старую скорость при смене направления. 0.05 = почти сразу остановить, 0.25 = мягче.")]
    [Range(0f, 1f)]
    [SerializeField] private float directionChangeBrakeStrength = 0.1f;

    [Tooltip("Минимальное ускорение по оси, чтобы считать это реальной сменой направления, а не шумом.")]
    [SerializeField] private float directionChangeAccelerationThresholdG = 0.006f;

    [Tooltip("Минимальная скорость по оси, при которой включается быстрое торможение смены направления.")]
    [SerializeField] private float directionChangeVelocityThreshold = 0.05f;

    [Header("IMU Reverse / Edge Assist")]
    [Tooltip("Не давать краткому тормозному ускорению сразу разворачивать скорость в обратную сторону. Сначала скорость гасится до нуля, обратное движение разрешается только если сигнал держится некоторое время.")]
    [SerializeField] private bool preventDirectionOvershoot = true;

    [Tooltip("Сколько секунд сигнал нового направления должен держаться, чтобы разрешить старт/разворот скорости. Больше = меньше случайный выбор направления.")]
    [SerializeField] private float reverseConfirmationTime = 0.12f;

    [Tooltip("Помогает выйти от края рабочей области даже при медленном движении датчика, когда ускорение маленькое.")]
    [SerializeField] private bool enableEdgeReleaseAssist = true;

    [Tooltip("Минимальный сырой сигнал ускорения в g для выхода от края. Должен быть меньше основной dead zone.")]
    [SerializeField] private float edgeReleaseAccelerationThresholdG = 0.0015f;

    [Tooltip("Минимальная скорость внутрь области при выходе от края, world units/sec.")]
    [SerializeField] private float edgeReleaseVelocity = 0.25f;


    [Header("IMU Direction Lock / Rest Start")]
    [Tooltip("Фиксировать первое направление движения на короткое время. Помогает не принимать тормозной импульс за новое направление.")]
    [SerializeField] private bool useImuDirectionLock = true;

    [Tooltip("Порог старта из состояния покоя. Должен быть меньше основной мёртвой зоны, чтобы медленное движение отлавливалось раньше.")]
    [SerializeField] private float restStartAccelerationThresholdG = 0.0012f;

    [Tooltip("Сколько секунд удерживать выбранное направление после старта движения.")]
    [SerializeField] private float directionLockTime = 0.35f;

    [Tooltip("Если включено, противоположный импульс во время direction lock считается торможением, но не разворачивает скорость.")]
    [SerializeField] private bool oppositeSignalBrakesOnlyDuringLock = true;


    [Header("IMU Simple Signed Velocity")]
    [Tooltip("Использовать простую знаковую скорость: acc > 0 разгоняет вправо, acc < 0 разгоняет влево. Без угадывания direction lock.")]
    [SerializeField] private bool useSimpleSignedVelocity = true;

    [Tooltip("Мягкое трение скорости, когда датчик движется. Меньше = скорость живёт дольше.")]
    [SerializeField] private float signedVelocityMovingFriction = 0.15f;

    [Tooltip("Трение скорости, когда ускорения почти нет и время coast уже прошло.")]
    [SerializeField] private float signedVelocityStopFriction = 2.5f;

    [Tooltip("Стабилизировать старт движения: коротко накапливать первые импульсы ускорения, чтобы стартовый тормозной/шумовой импульс не отправлял курсор в неправильную сторону.")]
    [SerializeField] private bool stabilizeSignedVelocityStart = true;

    [Tooltip("Окно подтверждения направления при старте из покоя, секунд. 0.06-0.14 обычно достаточно.")]
    [SerializeField] private float signedVelocityStartWindow = 0.10f;

    [Tooltip("Минимальный сигнал ускорения в g для начала подтверждения направления.")]
    [SerializeField] private float signedVelocityStartMinSignalG = 0.0015f;

    [Tooltip("Минимальная накопленная добавка скорости, после которой направление считается подтверждённым раньше окончания окна.")]
    [SerializeField] private float signedVelocityStartMinDelta = 0.015f;

    [Tooltip("Если включено, противоположный сигнал сначала тормозит текущую скорость до нуля и только потом разрешает движение в другую сторону.")]
    [SerializeField] private bool brakeBeforeReverse = true;

    [Tooltip("Пока модуль скорости больше этого значения, противоположное ускорение считается торможением, а не новым движением.")]
    [SerializeField] private float reverseStartVelocityThreshold = 0.15f;

    [Tooltip("Во сколько раз сильнее тормозить текущую скорость противоположным ускорением.")]
    [SerializeField] private float oppositeAccelerationBrakeMultiplier = 2.5f;

    [Tooltip("Требовать подтверждение обратного направления, чтобы одиночный тик не разворачивал скорость.")]
    [SerializeField] private bool requireReverseConfirmationForSignedVelocity = true;

    [Tooltip("Сколько секунд обратный сигнал должен держаться, чтобы разрешить движение в новую сторону.")]
    [SerializeField] private float signedVelocityReverseConfirmationTime = 0.12f;

    [Tooltip("Держать скорость на нуле до подтверждения обратного направления.")]
    [SerializeField] private bool holdVelocityAtZeroUntilReverseConfirmed = true;


    [Header("World Mapping")]
    [SerializeField] private Vector2 worldMin = new Vector2(-4f, -3f);
    [SerializeField] private Vector2 worldMax = new Vector2(4f, 3f);

    private SensorImuData latestImuData;

    private Vector2 rawSensorAngles;
    private Vector2 correctedAngles;
    private Vector2 targetWorldPosition;
    private Vector2 previousTargetWorldPosition;
    private Vector2 targetVelocity;
    private Vector2 filteredWorldPosition;

    private Vector3 rawAccelerationG;
    private Vector3 linearAccelerationG;
    private Vector3 rawGyroDegS;
    private float lastGyroMagnitudeDegS;
    private bool lastRotationSuppressed;
    private Vector3 accelerationBiasG;
    private bool hasAccelerationBias;
    private bool lastImuUsedAcceleration;
    private Vector2 imuVelocity;
    private Vector2 imuOffset;
    private Vector2 smoothedImuAcceleration;
    private float lastStrongImuSignalTime;
    private float lastImuVelocityLogTime;
    private Vector2 lastScreenSignalForDebug;
    private Vector2 lastScreenAccelerationForDebug;
    private Vector2 cleanPlanarFilteredSignal;
    private float xReverseSignalStartTime = -1f;
    private float yReverseSignalStartTime = -1f;
    private int xReverseSignalDirection = 0;
    private int yReverseSignalDirection = 0;
    private int xLockedDirection = 0;
    private int yLockedDirection = 0;
    private float xDirectionLockUntil = -1f;
    private float yDirectionLockUntil = -1f;
    private float signedXStartTime = -1f;
    private float signedYStartTime = -1f;
    private float signedXAccumulatedVelocityDelta = 0f;
    private float signedYAccumulatedVelocityDelta = 0f;
    private float signedXReverseStartTime = -1f;
    private float signedYReverseStartTime = -1f;
    private int signedXReverseDirection = 0;
    private int signedYReverseDirection = 0;

    private bool hasSensorData;
    private bool hasFilteredPosition;
    private bool isCalibrated;
    private float lastSensorPacketTime;
    private int lastSensorSequenceId = -1;

    public PointerInputSource InputSource => inputSource;
    public SensorControlMode SensorControlMode => sensorControlMode;
    public bool HasSensorData => hasSensorData;
    public Vector2 RawSensorAngles => rawSensorAngles;
    public Vector2 SensorCenter => sensorCenter;
    public Vector2 CorrectedAngles => correctedAngles;
    public Vector2 FilteredWorldPosition => filteredWorldPosition;
    public Vector2 TargetWorldPosition => targetWorldPosition;
    public Vector3 RawAccelerationG => rawAccelerationG;
    public Vector3 LinearAccelerationG => linearAccelerationG;
    public Vector3 RawGyroDegS => rawGyroDegS;
    public bool LastImuUsedAcceleration => lastImuUsedAcceleration;
    public Vector3 AccelerationBiasG => accelerationBiasG;
    public Vector2 ImuVelocity => imuVelocity;
    public Vector2 ImuOffset => imuOffset;

    private void Awake()
    {
        filteredWorldPosition = GetWorldCenter();
        targetWorldPosition = filteredWorldPosition;
        previousTargetWorldPosition = targetWorldPosition;
    }

    private void Update()
    {
        if (inputSource == PointerInputSource.Sensor &&
            calibrateByCKey &&
            Keyboard.current != null &&
            Keyboard.current.cKey.wasPressedThisFrame &&
            hasSensorData)
        {
            CalibrateSensorCenter();
        }

        if (inputSource == PointerInputSource.Sensor)
            UpdateVisualFiltering(Time.deltaTime);
    }

    public void Configure(PointerInputSource source)
    {
        inputSource = source;
    }

    // Совместимость со старым SensorUdpReceiver: roll/pitch.
    public void SetSensorAngles(Vector2 angles)
    {
        SensorImuData data = new SensorImuData
        {
            roll = angles.x,
            pitch = angles.y,
            yaw = 0f,
            accelerationG = rawAccelerationG,
            gyroDegS = rawGyroDegS,
            hasAcceleration = false,
            hasGyro = false,
            hasSequence = false,
            sequenceId = 0
        };

        SetSensorData(data);
    }

    public void SetSensorData(SensorImuData data)
    {
        // If UDP repeats the same BLE sample many times, do not integrate it again.
        // This is important for acceleration-based control: repeating old acceleration
        // creates false speed/drift/delay.
        if (data.hasSequence && data.sequenceId == lastSensorSequenceId)
            return;

        if (data.hasSequence)
            lastSensorSequenceId = data.sequenceId;

        float now = Time.time;
        float dt = 0f;

        if (lastSensorPacketTime > 0f)
            dt = now - lastSensorPacketTime;

        if (dt <= 0.0001f || dt > 0.5f)
            dt = Time.deltaTime > 0f ? Time.deltaTime : 0.016f;

        latestImuData = data;
        rawSensorAngles = new Vector2(data.roll, data.pitch);
        rawAccelerationG = data.accelerationG;
        rawGyroDegS = data.gyroDegS;
        hasSensorData = true;

        if (autoCalibrateOnFirstData && !isCalibrated)
            CalibrateSensorCenter();

        previousTargetWorldPosition = targetWorldPosition;

        if (sensorControlMode == SensorControlMode.ImuDisplacement)
        {
            if (HasUsableAcceleration(data))
                UpdateImuDisplacementTarget(dt);
            else if (fallbackToTiltIfNoUsableAcceleration)
            {
                lastImuUsedAcceleration = false;
                UpdateTiltTargetPosition();
            }
            else
            {
                lastImuUsedAcceleration = false;
                targetWorldPosition = GetWorldCenter() + imuOffset;
            }
        }
        else
        {
            lastImuUsedAcceleration = false;
            UpdateTiltTargetPosition();
        }

        if (lastSensorPacketTime > 0f && dt > 0.0001f && dt < 1f)
            targetVelocity = (targetWorldPosition - previousTargetWorldPosition) / dt;
        else
            targetVelocity = Vector2.zero;

        lastSensorPacketTime = now;

        if (!hasFilteredPosition)
        {
            filteredWorldPosition = targetWorldPosition;
            hasFilteredPosition = true;
        }
    }

    public void CalibrateSensorCenter()
    {
        sensorCenter = rawSensorAngles;
        correctedAngles = Vector2.zero;
        targetWorldPosition = GetWorldCenter();
        previousTargetWorldPosition = targetWorldPosition;
        targetVelocity = Vector2.zero;
        filteredWorldPosition = targetWorldPosition;
        hasFilteredPosition = true;
        isCalibrated = true;

        imuVelocity = Vector2.zero;
        imuOffset = Vector2.zero;
        smoothedImuAcceleration = Vector2.zero;
        lastStrongImuSignalTime = Time.time;
        linearAccelerationG = Vector3.zero;
        lastScreenSignalForDebug = Vector2.zero;
        lastScreenAccelerationForDebug = Vector2.zero;
        xLockedDirection = 0;
        yLockedDirection = 0;
        xDirectionLockUntil = -1f;
        yDirectionLockUntil = -1f;
        xReverseSignalDirection = 0;
        yReverseSignalDirection = 0;
        xReverseSignalStartTime = -1f;
        yReverseSignalStartTime = -1f;
        signedXStartTime = -1f;
        signedYStartTime = -1f;
        signedXAccumulatedVelocityDelta = 0f;
        signedYAccumulatedVelocityDelta = 0f;
        signedXReverseStartTime = -1f;
        signedYReverseStartTime = -1f;
        signedXReverseDirection = 0;
        signedYReverseDirection = 0;
        cleanPlanarFilteredSignal = Vector2.zero;

        if (hasSensorData && HasRawAcceleration(rawAccelerationG))
        {
            Vector3 estimatedGravity = removeGravityUsingAngles
                ? EstimateGravityFromAngles(rawSensorAngles.x, rawSensorAngles.y)
                : new Vector3(0f, 0f, 1f);

            accelerationBiasG = rawAccelerationG - estimatedGravity;
            hasAccelerationBias = true;
        }
        else
        {
            accelerationBiasG = Vector3.zero;
            hasAccelerationBias = false;
        }

        Debug.Log($"[Sensor] Calibrated center: roll={sensorCenter.x:F2}, pitch={sensorCenter.y:F2}, mode={sensorControlMode}, accBias={accelerationBiasG}");
    }

    public Vector2 GetPointerPosition()
    {
        if (inputSource == PointerInputSource.Sensor)
        {
            // В режиме Sensor нельзя fallback-иться на мышь, иначе кажется, что датчик не работает.
            if (!hasSensorData)
                return hasFilteredPosition ? filteredWorldPosition : GetWorldCenter();

            return filteredWorldPosition;
        }

        if (Mouse.current == null || Camera.main == null)
            return Vector2.zero;

        Vector2 mouse = Mouse.current.position.ReadValue();
        Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, 0f));
        world.z = 0f;
        return world;
    }

    public string GetSensorDebugText(float hz = -1f)
    {
        if (!hasSensorData)
            return "Датчик: нет данных";

        string hzPart = hz >= 0f ? $" | {hz:F0} Гц" : string.Empty;

        if (sensorControlMode == SensorControlMode.ImuDisplacement)
        {
            string accMode = lastImuUsedAcceleration ? (stableImuMotion ? "stable acc" : "acc") : "no acc→tilt";
            return $"Датчик: IMU {accMode} | roll {rawSensorAngles.x:F1}° | pitch {rawSensorAngles.y:F1}° | " +
                   $"rawA {rawAccelerationG.x:F2},{rawAccelerationG.y:F2},{rawAccelerationG.z:F2}g | " +
                   $"linA {linearAccelerationG.x:F3},{linearAccelerationG.y:F3}g | " +
                   $"V {imuVelocity.magnitude:F2} | X {filteredWorldPosition.x:F2} | Y {filteredWorldPosition.y:F2}{hzPart}";
        }

        return $"Датчик: Tilt | roll {rawSensorAngles.x:F1}° | pitch {rawSensorAngles.y:F1}° | " +
               $"X {filteredWorldPosition.x:F2} | Y {filteredWorldPosition.y:F2}{hzPart}";
    }

    private void UpdateTiltTargetPosition()
    {
        Vector2 delta = rawSensorAngles - sensorCenter;

        delta.x = ApplyDeadZone(delta.x, deadZoneDegrees);
        delta.y = ApplyDeadZone(delta.y, deadZoneDegrees);

        correctedAngles = delta;

        float rangeX = Mathf.Max(0.001f, Mathf.Abs(sensorRange.x));
        float rangeY = Mathf.Max(0.001f, Mathf.Abs(sensorRange.y));

        float nx = Mathf.Clamp(delta.x / rangeX, -1f, 1f);
        float ny = Mathf.Clamp(delta.y / rangeY, -1f, 1f);

        if (invertX)
            nx = -nx;
        if (invertY)
            ny = -ny;

        Vector2 center = GetWorldCenter();
        Vector2 halfSize = (worldMax - worldMin) * 0.5f;

        targetWorldPosition = new Vector2(
            center.x + nx * halfSize.x,
            center.y + ny * halfSize.y
        );
    }

    private void UpdateImuDisplacementTarget(float dt)
    {
        lastImuUsedAcceleration = true;
        lastRotationSuppressed = false;
        lastGyroMagnitudeDegS = rawGyroDegS.magnitude;

        Vector3 acc = rawAccelerationG;

        if (removeGravityUsingAngles)
            acc -= EstimateGravityFromAngles(rawSensorAngles.x, rawSensorAngles.y);
        else
            acc.z -= 1f;

        if (subtractAccelerationBias && hasAccelerationBias)
            acc -= accelerationBiasG;

        acc.x = ApplyDeadZone(acc.x, accelerationDeadZoneG);
        acc.y = ApplyDeadZone(acc.y, accelerationDeadZoneG);
        acc.z = ApplyDeadZone(acc.z, accelerationDeadZoneG);

        linearAccelerationG = acc;

        // Для экрана используем выбранные оси. Это важно: при движении датчика по столу
        // вертикальная составляющая часто ловит шум/гравитацию и курсор прыгает вверх-вниз.
        // rawScreenSignal сохраняем ДО основной dead zone: он нужен для мягкого выхода от края,
        // когда движение медленное и ускорение маленькое.
        float rawAx = ReadAccelerationAxis(acc, imuScreenXAxis);
        float rawAy = ReadAccelerationAxis(acc, imuScreenYAxis);

        float ax = ApplyDeadZone(rawAx, accelerationDeadZoneG);
        float ay = ApplyDeadZone(rawAy, accelerationDeadZoneG + Mathf.Max(0f, imuYExtraDeadZoneG));

        Vector2 rawScreenSignal = new Vector2(
            rawAx * Mathf.Max(0f, imuXScale),
            rawAy * Mathf.Max(0f, imuYScale)
        );

        Vector2 screenSignal = new Vector2(
            ax * Mathf.Max(0f, imuXScale),
            ay * Mathf.Max(0f, imuYScale)
        );

        if (suppressCrossAxisNoise && !lockImuYAxis)
            screenSignal = SuppressCrossAxisNoise(screenSignal);

        if (lockImuYAxis)
        {
            screenSignal.y = 0f;
            rawScreenSignal.y = 0f;
        }

        screenSignal = BoostSmallSignal(screenSignal);

        if (screenSignal.magnitude >= Mathf.Max(0.0001f, stopAccelerationThresholdG))
            lastStrongImuSignalTime = Time.time;

        if (invertX)
        {
            screenSignal.x = -screenSignal.x;
            rawScreenSignal.x = -rawScreenSignal.x;
        }
        if (invertY)
        {
            screenSignal.y = -screenSignal.y;
            rawScreenSignal.y = -rawScreenSignal.y;
        }

        ApplyRotationSuppressionToImuSignals(ref screenSignal, ref rawScreenSignal);

        // Физическая модель: ускорение -> скорость -> позиция.
        // Важно: если ускорение около нуля, скорость НЕ обнуляется сразу.
        // Курсор некоторое время продолжает движение с уже накопленной скоростью,
        // а затем плавно тормозит, чтобы не было бесконечного дрейфа от шума IMU.
        Vector2 screenAcceleration = screenSignal * accelerationToWorldScale * imuPositionGain;
        lastScreenSignalForDebug = screenSignal;
        lastScreenAccelerationForDebug = screenAcceleration;

        bool almostNoAcceleration = screenSignal.magnitude < Mathf.Max(0.0001f, stopAccelerationThresholdG);
        bool canCoast = Time.time - lastStrongImuSignalTime <= Mathf.Max(0f, imuCoastTime);

        if (useSimpleSignedVelocity)
        {
            // Чистая модель для основного не-наклонного режима.
            // Не угадываем направление через lock/timer.
            // Сглаживаем знак ускорения и используем скорость со знаком:
            // acc > 0 разгоняет в плюс, acc < 0 сначала тормозит старую скорость,
            // и только около нуля начинает движение в обратную сторону.
            float signalFilterAlpha = 1f - Mathf.Exp(-Mathf.Max(1f, imuVelocityResponse) * dt);
            cleanPlanarFilteredSignal = Vector2.Lerp(cleanPlanarFilteredSignal, screenSignal, signalFilterAlpha);

            IntegrateCleanPlanarVelocityAxis(
                ref imuVelocity.x,
                cleanPlanarFilteredSignal.x,
                dt
            );

            if (!lockImuYAxis)
            {
                IntegrateCleanPlanarVelocityAxis(
                    ref imuVelocity.y,
                    cleanPlanarFilteredSignal.y,
                    dt
                );
            }

            if (maxImuVelocity > 0f)
                imuVelocity = Vector2.ClampMagnitude(imuVelocity, maxImuVelocity);
        }
        else
        {
            // Старый экспериментальный режим оставлен как запасной.
            ApplyDirectionChangeBrake(screenSignal);

            if (stableImuMotion)
            {
                float responseAlpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, imuVelocityResponse) * dt);
                smoothedImuAcceleration = Vector2.Lerp(smoothedImuAcceleration, screenAcceleration, responseAlpha);

                if (!almostNoAcceleration)
                {
                    IntegrateImuVelocityWithReverseGuard(smoothedImuAcceleration, screenSignal, dt);
                    float movingDamping = Mathf.Max(0f, velocityDamping) * 0.15f;
                    imuVelocity *= Mathf.Exp(-movingDamping * dt);
                }
                else
                {
                    smoothedImuAcceleration = Vector2.Lerp(smoothedImuAcceleration, Vector2.zero, responseAlpha);

                    if (stopWhenAccelerationIsSmall)
                    {
                        float damping = canCoast ? Mathf.Max(0f, velocityDamping) * 0.25f : Mathf.Max(0f, imuStopDamping);
                        imuVelocity *= Mathf.Exp(-damping * dt);

                        if (!canCoast && imuVelocity.magnitude < zeroVelocityThreshold)
                            imuVelocity = Vector2.zero;
                    }
                }

                if (maxImuVelocity > 0f)
                    imuVelocity = Vector2.ClampMagnitude(imuVelocity, maxImuVelocity);
            }
            else
            {
                imuVelocity += screenAcceleration * dt;
                imuVelocity *= Mathf.Exp(-Mathf.Max(0f, velocityDamping) * dt);

                if (imuVelocity.magnitude < zeroVelocityThreshold && screenAcceleration.magnitude < accelerationDeadZoneG * 2f)
                    imuVelocity = Vector2.zero;

                if (maxImuVelocity > 0f)
                    imuVelocity = Vector2.ClampMagnitude(imuVelocity, maxImuVelocity);
            }
        }

        if (lockImuYAxis)
            imuVelocity.y = 0f;

        ApplyEdgeReleaseAssist(GetWorldCenter(), rawScreenSignal);

        imuOffset += imuVelocity * dt;

        if (lockImuYAxis && keepLockedYAtCenter)
            imuOffset.y = 0f;

        Vector2 center = GetWorldCenter();
        ClampImuOffsetAndVelocityToBounds(center);

        targetWorldPosition = new Vector2(center.x + imuOffset.x, center.y + imuOffset.y);
        targetWorldPosition.x = Mathf.Clamp(targetWorldPosition.x, worldMin.x, worldMax.x);
        targetWorldPosition.y = Mathf.Clamp(targetWorldPosition.y, worldMin.y, worldMax.y);

        // Важно: после финального clamp синхронизируем скрытый offset с видимой позицией.
        // Иначе offset может оставаться за экраном, и при движении обратно придётся долго "отматывать" невидимое смещение.
        if (clampImuOffsetToWorldBounds)
            imuOffset = targetWorldPosition - center;

        LogImuVelocityIfNeeded(screenSignal, screenAcceleration, dt, almostNoAcceleration, canCoast);
    }


    private bool ProcessSignedVelocityStartAxis(
        ref float velocity,
        float acceleration,
        float signal,
        ref float startTime,
        ref float accumulatedVelocityDelta,
        float dt
    )
    {
        if (!stabilizeSignedVelocityStart)
            return false;

        float velocityThreshold = Mathf.Max(0.000001f, directionChangeVelocityThreshold);
        float signalThreshold = Mathf.Max(0.000001f, signedVelocityStartMinSignalG);

        // Если ось уже уверенно движется, работаем обычной интеграцией.
        if (Mathf.Abs(velocity) >= velocityThreshold)
        {
            startTime = -1f;
            accumulatedVelocityDelta = 0f;
            return false;
        }

        // Если движения по оси нет, ничего не стартуем.
        if (Mathf.Abs(signal) < signalThreshold)
        {
            startTime = -1f;
            accumulatedVelocityDelta = 0f;
            return true;
        }

        float now = Time.time;

        if (startTime < 0f)
        {
            startTime = now;
            accumulatedVelocityDelta = 0f;
        }

        accumulatedVelocityDelta += acceleration * dt;

        float window = Mathf.Max(0.01f, signedVelocityStartWindow);
        float minDelta = Mathf.Max(0f, signedVelocityStartMinDelta);
        bool enoughTime = now - startTime >= window;
        bool enoughImpulse = minDelta > 0f && Mathf.Abs(accumulatedVelocityDelta) >= minDelta;

        if (enoughTime || enoughImpulse)
        {
            velocity += accumulatedVelocityDelta;

            if (maxImuVelocity > 0f)
                velocity = Mathf.Clamp(velocity, -maxImuVelocity, maxImuVelocity);

            startTime = -1f;
            accumulatedVelocityDelta = 0f;
        }

        // Пока идёт окно подтверждения, обычную интеграцию не выполняем.
        return true;
    }


    private void IntegrateSimpleSignedVelocityAxis(
        ref float velocity,
        float acceleration,
        float signal,
        ref float reverseStartTime,
        ref int reverseDirection,
        float dt
    )
    {
        // Простая знаковая скорость по одной оси.
        // signal > 0 разгоняет в плюс, signal < 0 разгоняет в минус.
        // Важный фикс: один случайный тик противоположного сигнала НЕ разворачивает скорость.
        // Сначала скорость тормозится к нулю, затем противоположный сигнал должен подтвердиться короткое время.
        if (dt <= 0f)
            return;

        float signalThreshold = Mathf.Max(0.000001f, accelerationDeadZoneG);
        if (Mathf.Abs(signal) < signalThreshold)
        {
            reverseStartTime = -1f;
            reverseDirection = 0;
            return;
        }

        int signalDirection = signal > 0f ? 1 : -1;
        float reverseVelocityThreshold = Mathf.Max(0f, reverseStartVelocityThreshold);
        float smallVelocityThreshold = Mathf.Max(0.000001f, zeroVelocityThreshold);
        bool hasVelocity = Mathf.Abs(velocity) > smallVelocityThreshold;
        bool oppositeDirection = hasVelocity && Mathf.Sign(velocity) != signalDirection;

        if (brakeBeforeReverse && oppositeDirection)
        {
            float brakeAmount = Mathf.Abs(acceleration) * Mathf.Max(0f, oppositeAccelerationBrakeMultiplier) * dt;
            velocity = Mathf.MoveTowards(velocity, 0f, brakeAmount);

            // Пока старая скорость ещё заметная, только тормозим.
            if (Mathf.Abs(velocity) > reverseVelocityThreshold)
            {
                reverseStartTime = -1f;
                reverseDirection = 0;
                return;
            }

            // Скорость уже около нуля. Теперь не даём одиночному тику сразу запустить обратное движение.
            if (requireReverseConfirmationForSignedVelocity)
            {
                if (reverseDirection != signalDirection || reverseStartTime < 0f)
                {
                    reverseDirection = signalDirection;
                    reverseStartTime = Time.time;
                }

                float confirmTime = Mathf.Max(0f, signedVelocityReverseConfirmationTime);
                bool confirmed = Time.time - reverseStartTime >= confirmTime;

                if (!confirmed)
                {
                    if (holdVelocityAtZeroUntilReverseConfirmed)
                        velocity = 0f;
                    return;
                }
            }
        }
        else
        {
            reverseStartTime = -1f;
            reverseDirection = 0;
        }

        velocity += acceleration * dt;

        if (maxImuVelocity > 0f)
            velocity = Mathf.Clamp(velocity, -maxImuVelocity, maxImuVelocity);
    }


    private void IntegrateCleanPlanarVelocityAxis(
        ref float velocity,
        float signal,
        float dt
    )
    {
        // Clean planar signed velocity:
        // signal > 0 разгоняет скорость в плюс, signal < 0 — в минус.
        // Если сигнал противоположен текущей скорости, сначала тормозим к нулю,
        // а не перекидываем скорость сразу на другую сторону.
        if (dt <= 0f)
            return;

        float signalThreshold = Mathf.Max(0.000001f, accelerationDeadZoneG);
        bool hasSignal = Mathf.Abs(signal) >= signalThreshold;

        if (!hasSignal)
        {
            float friction = Mathf.Max(0f, signedVelocityStopFriction);
            velocity = Mathf.MoveTowards(velocity, 0f, friction * dt);

            if (Mathf.Abs(velocity) < Mathf.Max(0.000001f, zeroVelocityThreshold))
                velocity = 0f;

            return;
        }

        float acceleration = signal * accelerationToWorldScale * imuPositionGain;
        int signalDirection = signal > 0f ? 1 : -1;
        bool hasVelocity = Mathf.Abs(velocity) > Mathf.Max(0.000001f, zeroVelocityThreshold);
        bool oppositeDirection = hasVelocity && Mathf.Sign(velocity) != signalDirection;

        if (brakeBeforeReverse && oppositeDirection)
        {
            float brakeAmount = Mathf.Abs(acceleration) * Mathf.Max(0f, oppositeAccelerationBrakeMultiplier) * dt;
            velocity = Mathf.MoveTowards(velocity, 0f, brakeAmount);

            // Пока старая скорость заметная, противоположный сигнал только тормозит.
            if (Mathf.Abs(velocity) > Mathf.Max(0f, reverseStartVelocityThreshold))
                return;
        }

        velocity += acceleration * dt;

        float movingFriction = Mathf.Max(0f, signedVelocityMovingFriction);
        if (movingFriction > 0f)
            velocity = Mathf.MoveTowards(velocity, 0f, movingFriction * dt);

        if (maxImuVelocity > 0f)
            velocity = Mathf.Clamp(velocity, -maxImuVelocity, maxImuVelocity);
    }


    private void IntegrateImuVelocityWithReverseGuard(Vector2 acceleration, Vector2 signal, float dt)
    {
        IntegrateVelocityAxis(
            ref imuVelocity.x,
            acceleration.x,
            signal.x,
            ref xReverseSignalStartTime,
            ref xReverseSignalDirection,
            ref xLockedDirection,
            ref xDirectionLockUntil,
            dt
        );

        IntegrateVelocityAxis(
            ref imuVelocity.y,
            acceleration.y,
            signal.y,
            ref yReverseSignalStartTime,
            ref yReverseSignalDirection,
            ref yLockedDirection,
            ref yDirectionLockUntil,
            dt
        );
    }

    private void IntegrateVelocityAxis(
        ref float velocity,
        float acceleration,
        float signal,
        ref float reverseSignalStartTime,
        ref int reverseSignalDirection,
        ref int lockedDirection,
        ref float directionLockUntil,
        float dt
    )
    {
        if (!preventDirectionOvershoot)
        {
            velocity += acceleration * dt;
            return;
        }

        float startThreshold = Mathf.Max(0.000001f, restStartAccelerationThresholdG);
        float accThreshold = Mathf.Max(startThreshold, directionChangeAccelerationThresholdG);
        float velThreshold = Mathf.Max(0.000001f, directionChangeVelocityThreshold);
        float signalAbs = Mathf.Abs(signal);
        float velocityAbs = Mathf.Abs(velocity);
        float now = Time.time;

        if (signalAbs < startThreshold)
        {
            reverseSignalStartTime = -1f;
            reverseSignalDirection = 0;

            if (useImuDirectionLock && now > directionLockUntil)
                lockedDirection = 0;

            return;
        }

        int signalDir = signal > 0f ? 1 : -1;
        bool lockActive = useImuDirectionLock && lockedDirection != 0 && now <= directionLockUntil;

        // Если скорость почти нулевая, стартуем СРАЗУ от первого слабого сигнала.
        // В старой версии здесь было ожидание reverseConfirmationTime, из-за чего скорость
        // почти всегда была 0, а потом появлялся один большой рывок.
        if (velocityAbs < velThreshold)
        {
            if (lockActive && oppositeSignalBrakesOnlyDuringLock && signalDir != lockedDirection)
            {
                velocity = 0f;
                return;
            }

            velocity += acceleration * dt;

            if (useImuDirectionLock)
            {
                lockedDirection = signalDir;
                directionLockUntil = now + Mathf.Max(0f, directionLockTime);
            }

            reverseSignalStartTime = -1f;
            reverseSignalDirection = 0;
            return;
        }

        int velocityDir = velocity > 0f ? 1 : -1;

        // Сигнал в сторону текущей скорости — обычный разгон.
        if (signalDir == velocityDir)
        {
            velocity += acceleration * dt;

            if (useImuDirectionLock)
            {
                lockedDirection = signalDir;
                directionLockUntil = now + Mathf.Max(0f, directionLockTime);
            }

            reverseSignalStartTime = -1f;
            reverseSignalDirection = 0;
            return;
        }

        // Противоположный сигнал при активном lock — это торможение, а не новое направление.
        // Это убирает ситуацию, когда при движении влево стоп-импульс случайно отправляет курсор вправо.
        if (lockActive && oppositeSignalBrakesOnlyDuringLock)
        {
            float proposedVelocity = velocity + acceleration * dt;
            bool crossedZero = Mathf.Sign(proposedVelocity) != velocityDir || Mathf.Abs(proposedVelocity) < velThreshold;
            velocity = crossedZero ? 0f : proposedVelocity;
            return;
        }

        // Дальше стандартная логика: противоположный сигнал сначала гасит скорость до нуля,
        // и только если держится достаточно долго — разрешает разворот.
        float proposed = velocity + acceleration * dt;
        bool crossed = Mathf.Sign(proposed) != velocityDir || Mathf.Abs(proposed) < velThreshold;

        if (!crossed)
        {
            velocity = proposed;
            return;
        }

        velocity = 0f;

        if (signalAbs < accThreshold)
        {
            reverseSignalStartTime = -1f;
            reverseSignalDirection = 0;
            return;
        }

        if (reverseSignalDirection != signalDir || reverseSignalStartTime < 0f)
        {
            reverseSignalDirection = signalDir;
            reverseSignalStartTime = now;
        }

        bool reverseConfirmed = now - reverseSignalStartTime >= Mathf.Max(0f, reverseConfirmationTime);

        if (reverseConfirmed)
        {
            velocity += acceleration * dt;

            if (useImuDirectionLock)
            {
                lockedDirection = signalDir;
                directionLockUntil = now + Mathf.Max(0f, directionLockTime);
            }
        }
    }

    private void ApplyEdgeReleaseAssist(Vector2 center, Vector2 rawScreenSignal)
    {
        if (!enableEdgeReleaseAssist)
            return;

        float threshold = Mathf.Max(0.000001f, edgeReleaseAccelerationThresholdG);
        float releaseVelocity = Mathf.Max(0f, edgeReleaseVelocity);
        if (releaseVelocity <= 0f)
            return;

        GetImuOffsetLimits(center, out float minX, out float maxX, out float minY, out float maxY);
        float eps = Mathf.Max(0.000001f, imuBoundsEpsilon);

        bool atLeft = imuOffset.x <= minX + eps;
        bool atRight = imuOffset.x >= maxX - eps;
        bool atBottom = imuOffset.y <= minY + eps;
        bool atTop = imuOffset.y >= maxY - eps;

        // Если курсор у края и появился даже слабый сигнал внутрь области,
        // даём небольшой старт скорости внутрь. Это убирает ситуацию, когда
        // от края можно уйти только резким рывком.
        if (atRight && rawScreenSignal.x < -threshold)
            imuVelocity.x = Mathf.Min(imuVelocity.x, -releaseVelocity);
        else if (atLeft && rawScreenSignal.x > threshold)
            imuVelocity.x = Mathf.Max(imuVelocity.x, releaseVelocity);

        if (!lockImuYAxis)
        {
            if (atTop && rawScreenSignal.y < -threshold)
                imuVelocity.y = Mathf.Min(imuVelocity.y, -releaseVelocity);
            else if (atBottom && rawScreenSignal.y > threshold)
                imuVelocity.y = Mathf.Max(imuVelocity.y, releaseVelocity);
        }
    }

    private void GetImuOffsetLimits(Vector2 center, out float minOffsetX, out float maxOffsetX, out float minOffsetY, out float maxOffsetY)
    {
        float maxOffsetXAbs = Mathf.Abs(maxImuOffset.x);
        float maxOffsetYAbs = Mathf.Abs(maxImuOffset.y);

        minOffsetX = -maxOffsetXAbs;
        maxOffsetX = maxOffsetXAbs;
        minOffsetY = -maxOffsetYAbs;
        maxOffsetY = maxOffsetYAbs;

        if (clampImuOffsetToWorldBounds)
        {
            minOffsetX = Mathf.Max(minOffsetX, worldMin.x - center.x);
            maxOffsetX = Mathf.Min(maxOffsetX, worldMax.x - center.x);
            minOffsetY = Mathf.Max(minOffsetY, worldMin.y - center.y);
            maxOffsetY = Mathf.Min(maxOffsetY, worldMax.y - center.y);
        }
    }


    private void ApplyDirectionChangeBrake(Vector2 screenSignal)
    {
        if (!fastBrakeOnDirectionChange)
            return;

        float accThreshold = Mathf.Max(0.000001f, directionChangeAccelerationThresholdG);
        float velThreshold = Mathf.Max(0.000001f, directionChangeVelocityThreshold);
        float strength = Mathf.Clamp01(directionChangeBrakeStrength);

        // Если новый сигнал ускорения уже направлен в противоположную сторону,
        // а текущая скорость ещё тянет курсор по старому направлению,
        // быстро гасим старую скорость. Иначе при развороте курсор долго "докатывается" назад.
        if (Mathf.Abs(screenSignal.x) >= accThreshold && Mathf.Abs(imuVelocity.x) >= velThreshold)
        {
            if (Mathf.Sign(screenSignal.x) != Mathf.Sign(imuVelocity.x))
                imuVelocity.x *= strength;
        }

        if (Mathf.Abs(screenSignal.y) >= accThreshold && Mathf.Abs(imuVelocity.y) >= velThreshold)
        {
            if (Mathf.Sign(screenSignal.y) != Mathf.Sign(imuVelocity.y))
                imuVelocity.y *= strength;
        }
    }

    private void ClampImuOffsetAndVelocityToBounds(Vector2 center)
    {
        GetImuOffsetLimits(center, out float minOffsetX, out float maxAllowedOffsetX, out float minOffsetY, out float maxAllowedOffsetY);

        float beforeX = imuOffset.x;
        float beforeY = imuOffset.y;

        imuOffset.x = Mathf.Clamp(imuOffset.x, minOffsetX, maxAllowedOffsetX);
        imuOffset.y = Mathf.Clamp(imuOffset.y, minOffsetY, maxAllowedOffsetY);

        if (!stopImuVelocityAtBounds)
            return;

        float eps = Mathf.Max(0.000001f, imuBoundsEpsilon);

        bool hitLeft = beforeX < minOffsetX || imuOffset.x <= minOffsetX + eps;
        bool hitRight = beforeX > maxAllowedOffsetX || imuOffset.x >= maxAllowedOffsetX - eps;
        bool hitBottom = beforeY < minOffsetY || imuOffset.y <= minOffsetY + eps;
        bool hitTop = beforeY > maxAllowedOffsetY || imuOffset.y >= maxAllowedOffsetY - eps;

        // По умолчанию скорость на границе НЕ зануляем: ограничиваем только координату.
        // Жёсткое зануление ломает IMU-управление: при выходе от края приходится заново "раскачивать" скорость.
        // Если stopImuVelocityAtBounds включён вручную, мягко гасим только скорость, которая давит наружу.
        const float boundsBrakeFactor = 0.35f;

        if ((hitLeft && imuVelocity.x < 0f) || (hitRight && imuVelocity.x > 0f))
            imuVelocity.x *= boundsBrakeFactor;

        if ((hitBottom && imuVelocity.y < 0f) || (hitTop && imuVelocity.y > 0f))
            imuVelocity.y *= boundsBrakeFactor;
    }

    private void LogImuVelocityIfNeeded(Vector2 screenSignal, Vector2 screenAcceleration, float dt, bool almostNoAcceleration, bool canCoast)
    {
        if (!logImuVelocityToConsole)
            return;

        float interval = Mathf.Max(0.02f, imuVelocityLogInterval);
        if (Time.time - lastImuVelocityLogTime < interval)
            return;

        float speed = imuVelocity.magnitude;
        float accMagnitude = screenSignal.magnitude;

        bool passesMovingFilter =
            speed >= Mathf.Max(0f, imuVelocityLogMinSpeed) ||
            accMagnitude >= Mathf.Max(0f, imuVelocityLogMinAccelerationG);

        // Важно: даже если logOnlyWhenImuMoving включён, всё равно печатаем строку.
        // Иначе кажется, что лог сломался, когда фильтр считает датчик неподвижным.
        // Статус MOVING/IDLE показывает, прошёл ли сигнал порог движения.
        lastImuVelocityLogTime = Time.time;
        string motionState = passesMovingFilter ? "MOVING" : "IDLE";

        Debug.Log(
            $"[IMU SPEED {motionState}] " +
            $"dt={dt:F3}s | " +
            $"rawA=({rawAccelerationG.x:F3},{rawAccelerationG.y:F3},{rawAccelerationG.z:F3})g | " +
            $"linA=({linearAccelerationG.x:F3},{linearAccelerationG.y:F3},{linearAccelerationG.z:F3})g | " +
            $"screenA=({screenSignal.x:F4},{screenSignal.y:F4})g | " +
            $"worldAcc=({screenAcceleration.x:F3},{screenAcceleration.y:F3}) | " +
            $"vel=({imuVelocity.x:F3},{imuVelocity.y:F3}) | speed={speed:F3} | " +
            $"offset=({imuOffset.x:F3},{imuOffset.y:F3}) | " +
            $"pos=({targetWorldPosition.x:F2},{targetWorldPosition.y:F2}) | " +
            $"dirLock=({xLockedDirection},{yLockedDirection}) | " +
            $"startAcc=({signedXAccumulatedVelocityDelta:F3},{signedYAccumulatedVelocityDelta:F3}) | " +
            $"noAcc={almostNoAcceleration} | coast={canCoast}"
        );
    }

    private Vector2 BoostSmallSignal(Vector2 signal)
    {
        float boost = Mathf.Max(1f, imuSmallSignalBoost);
        if (boost <= 1.001f)
            return signal;

        // Усиливаем именно малые импульсы. Большие не раздуваем бесконечно.
        float magnitude = signal.magnitude;
        if (magnitude <= 0.0001f)
            return signal;

        float boostedMagnitude = Mathf.Min(magnitude * boost, magnitude + 0.08f);
        return signal.normalized * boostedMagnitude;
    }

    private Vector2 SuppressCrossAxisNoise(Vector2 signal)
    {
        float ax = Mathf.Abs(signal.x);
        float ay = Mathf.Abs(signal.y);
        float ratio = Mathf.Max(1.01f, crossAxisDominanceRatio);

        if (ax > ay * ratio)
            signal.y = 0f;
        else if (ay > ax * ratio)
            signal.x = 0f;

        return signal;
    }

    private void ApplyRotationSuppressionToImuSignals(ref Vector2 screenSignal, ref Vector2 rawScreenSignal)
    {
        lastGyroMagnitudeDegS = rawGyroDegS.magnitude;
        lastRotationSuppressed = false;

        if (!ignoreAccelerationWhileRotating)
            return;

        float threshold = Mathf.Max(0f, rotationGyroThresholdDegS);
        if (threshold <= 0f)
            return;

        if (lastGyroMagnitudeDegS < threshold)
            return;

        float suppression = Mathf.Clamp01(rotationAccelerationSuppression);
        screenSignal *= suppression;
        rawScreenSignal *= suppression;
        lastRotationSuppressed = true;

        if (logRotationSuppression)
        {
            Debug.Log($"[IMU ROTATION] gyro={lastGyroMagnitudeDegS:F1} deg/s > {threshold:F1}; acc suppressed x{suppression:F2}");
        }
    }

    private float ReadAccelerationAxis(Vector3 acc, ImuAccelerationAxis axis)
    {
        switch (axis)
        {
            case ImuAccelerationAxis.AccX:
                return acc.x;
            case ImuAccelerationAxis.AccY:
                return acc.y;
            case ImuAccelerationAxis.AccZ:
                return acc.z;
            case ImuAccelerationAxis.NegativeAccX:
                return -acc.x;
            case ImuAccelerationAxis.NegativeAccY:
                return -acc.y;
            case ImuAccelerationAxis.NegativeAccZ:
                return -acc.z;
            default:
                return acc.x;
        }
    }

    private bool HasUsableAcceleration(SensorImuData data)
    {
        if (!data.hasAcceleration)
            return false;

        if (treatZeroAccelerationAsMissing && !HasRawAcceleration(data.accelerationG))
            return false;

        return true;
    }

    private bool HasRawAcceleration(Vector3 acc)
    {
        return acc.sqrMagnitude > 0.000001f;
    }

    private Vector3 EstimateGravityFromAngles(float rollDeg, float pitchDeg)
    {
        // Приближённая модель направления гравитации в координатах датчика.
        // Она нужна не для точной навигации, а чтобы убрать основную постоянную 1g-составляющую.
        float roll = rollDeg * Mathf.Deg2Rad;
        float pitch = pitchDeg * Mathf.Deg2Rad;

        float gx = -Mathf.Sin(pitch);
        float gy = Mathf.Sin(roll) * Mathf.Cos(pitch);
        float gz = Mathf.Cos(roll) * Mathf.Cos(pitch);

        return new Vector3(gx, gy, gz);
    }

    private void UpdateVisualFiltering(float deltaTime)
    {
        if (!hasSensorData || !hasFilteredPosition || deltaTime <= 0f)
            return;

        Vector2 predictedTarget = targetWorldPosition + targetVelocity * predictionTime;
        predictedTarget.x = Mathf.Clamp(predictedTarget.x, worldMin.x, worldMax.x);
        predictedTarget.y = Mathf.Clamp(predictedTarget.y, worldMin.y, worldMax.y);

        float alpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, followSpeed) * deltaTime);
        Vector2 next = Vector2.Lerp(filteredWorldPosition, predictedTarget, alpha);

        if (maxVisualSpeed > 0f)
        {
            float maxStep = maxVisualSpeed * deltaTime;
            next = Vector2.MoveTowards(filteredWorldPosition, next, maxStep);
        }

        filteredWorldPosition = next;
    }

    private float ApplyDeadZone(float value, float deadZone)
    {
        float abs = Mathf.Abs(value);
        if (abs <= deadZone)
            return 0f;

        return Mathf.Sign(value) * (abs - deadZone);
    }

    private Vector2 GetWorldCenter()
    {
        return (worldMin + worldMax) * 0.5f;
    }
}
