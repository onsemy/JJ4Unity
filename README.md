# JJ4Unity

Unity를 쓰면서 유용한 기능을 모은 저장소

## 설치 방법

1. Package Manager에서 `+`를 눌러서 `Add package from git URL...`을 선택합니다.
2. 다음 주소를 복사/붙여넣기 합니다.
    > `https://github.com/onsemy/JJ4Unity.git?path=Assets/JJ4Unity`

## 예제 코드

`Assets/Scenes/SampleScene.unity`에서 실행하여 확인 할 수 있습니다. 예제 코드는 `Assets/Test.cs`에서 확인 가능하고, `Test.cs`가 부착된 게임 오브젝트는 `Main Camera` 입니다.

## 주요 기능

### Addressables 암/복호화 추가

기본적인 Addressables는 암호화 및 복호화하지 않고 불러와서 애써 만든 소중한 에셋이 탈취 당할 가능성이 높습니다. 해커를 조금 귀찮게 해줄 암/복호화 기능을 추가했습니다. 프로젝트에서 사용하기 위해 다음과 같은 코드를 Addressables를 사용하기 전에 호출해주세요.

> `JJ4Unity.Runtime.AssetBundle.AddressablesHelper.SetEncryptedProvider(new EncryptedAssetBundleProvider(AES_Key, AES_IV));`

암/복호화 알고리즘은 AES로 되어 있으나, 원한다면 `EncryptedAssetBundleProvider`를 상속받아서 적용할 수 있습니다. 본인만의 클래스를 구현한 후, `SetEncryptedProvider()` 함수를 본인의 클래스를 넣어주세요.

위와 같은 암/복호화를 사용하는 경우, Addressables 메뉴에서 제공하는 빌드가 아닌 직접 빌드 스크립트를 작성해야 합니다. JJ4Unity에서는 `JJ4Unity/Build AssetBundle` 메뉴를 통해 Addressables Groups에 설정된 번들을 빌드 및 암호화할 수 있습니다. AES Key 및 IV 설정은 `JJ4Unity/Open Encrypt AssetBundle Settings`에서 가능합니다.

### Debug

UnityEngine.Debug를 대체합니다. Scripting Define에 `__DEBUG__`가 선언되어 있는 경우에만 출력됩니다. 로그를 호출하는 경우 다음과 같이 출력됩니다.

> `[파일명::클래스명:L줄번호:T프레임카운트] 로그내용`

### AssignPath

런타임 또는 에디터에서 `public` 또는 `[SerializeField]`로 선언된 멤버 변수를 자동으로 할당해주는 기능입니다.

```csharp
// 런타임에서 사용할 경우
using JJ4Unity.Runtime.Attribute;
using JJ4Unity.Runtime.Extension;

public class SomeClass1 : MonoBehaviour
{
    [SerializeField, AssignPath] private SpriteRenderer _front;

    private void Awake()
    {
        // 아래 함수를 반드시 실행해야 합니다.
        this.AssignPaths();
    }
}
```

```csharp
// 에디터에서 사용할 경우
using JJ4Unity.Runtime.Attribute;

public class SomeClass2 : MonoBehaviour
{
    [SerializeField, AssignPath(true)] private SpriteRenderer _front;
}

// 이후 Unity Editor에서 해당 컴포넌트가 추가된 게임 오브젝트를
// 선택하여 인스펙터에서 [Assign Variables] 버튼을 누릅니다.
```

![](docs/2024-11-20-02-10-53.png)

### Visual Studio Code 연동

Visual Studio Code를 코드 편집기로 사용하는 경우, [JJ4UnityVSC](https://github.com/onsemy/JJ4UnityVSC) 확장 프로그램을 설치한 이후 코드 편집기 내에서 C# 스크립트를 편집 후 저장하는 경우 Unity Editor가 자동으로 갱신되며 컴파일이 시작됩니다. (JetBrains Rider에서 제공하는 기능과 유사하다고 보시면 됩니다.)

해당 기능을 끄고 싶다면, 상단 메뉴바의 `JJ4Unity/Toggle Connect to VSCode`를 선택하여 토글해주세요.

### Singleton/MonoBehaviour Singleton

일반적인 `Singleton<T>`과 MonoBehaviour를 위한 `MonoSingleton<T>`을 쓸 수 있습니다.

### ReadOnly

`public` 또는 `[SerializeField]`로 선언된 멤버 변수를 유니티 에디터의 인스펙터에서 수정할 수 없도록 합니다.

```csharp
using JJ4Unity.Runtime.Attribute;

public class SomeClass3 : MonoBehaviour
{
    [SerializeField, ReadOnly] private int _readOnlyInt = 10;
    [ReadOnly] public string _readOnlyStringValue = "ReadOnlyStringValue";
}
```

## 알려진 문제

### Visual Studio Code 연동 기능이 하나의 Unity Editor에서만 사용 가능

Unity Editor와 Visual Studio Code가 1:1로 하나만 연동됩니다. 추후 다중 연결을 지원할 예정입니다.
