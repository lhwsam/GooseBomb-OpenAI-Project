# 외부 UI 패키지

Asset Store나 다른 공급처에서 가져온 UI 패키지 원본은 공급자별 하위 폴더에 둔다.

```text
Assets/ThirdParty/UI/
  <PublisherOrPackageName>/
```

- 공급자 파일은 직접 수정하지 않는다.
- 패키지가 고정된 자체 경로로 import된다면 억지로 이동하지 않고 그 경로를 유지한다.
- 게임용 수정은 `Assets/Game/Content/UI`의 복사본 또는 Prefab Variant에서 수행한다.
- 샘플 씬, 데모 코드, 사용하지 않는 고해상도 원본은 Build Settings와 Resources 경로에 넣지 않는다.
- 라이선스와 출처를 각 패키지 폴더의 `LICENSE` 또는 `NOTICE` 파일로 보존한다.
