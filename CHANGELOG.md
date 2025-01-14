# Changelog

## [0.1.3] - 2025-01-15

- EncryptedAssetBundleProvider에서 UnityWebRequest.DownloadHandler를 기본 설정으로 쓰도록 변경
- DecryptedBundleResource에 임시 저장용 Stream을 설정할 수 있도록 추가
- 쓰지 않는 코드 정리

## [0.1.2] - 2025-01-15

- EncryptedAssetBundleProvider에서 특정 상황에 예외가 발생하는 문제 수정
- EncryptedAssetBundleProvider 사용 시 Stream과 Byte 배열 로드 방식을 선택 가능하도록 작업
- 암호화 번들이 Addressables.Release에도 메모리에서 해제되지 않는 문제 수정

## [0.1.1] - 2025-01-07

- 쓰지 않는 코드 정리
- README 설명 링크 수정

## [0.1.0] - 2025-01-07

- 유니티6 (6000.0) 이상부터 사용 가능하도록 작업
- Addressables에 암/복호화 사용 가능하도록 작업
- JJ4UnitySettings 추가

## [0.0.5] - 2024-11-20

- UnityEngine.Debug를 쓰던 내부 코드를 JJ4Unity.Extension.Debug를 쓰도록 변경
- 패키지 정보에 Documentation, ChangeLog URL이 첨부되도록 추가

## [0.0.4] - 2024-11-20

- ReadOnly 추가

## [0.0.3] - 2024-11-20

- JJ4UnityVSC 연동 설정 추가
- Singleton/MonoSingleton 클래스 추가

## [0.0.2] - 2024-10-22

- AssignPath 기능 수정
- JJ4UnityVSC 연동 작업 추가
