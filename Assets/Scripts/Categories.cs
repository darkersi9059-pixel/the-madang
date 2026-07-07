// 수집 카테고리: 동물 종류(AnimalType)를 사람이 보는 카테고리명으로 매핑하는 단일 출처.
// 앨범/도감/사진저장 모두 이 매핑을 공유해 카테고리 표기가 어긋나지 않게 함.
public static class Categories
{
    // 탭/목록에 보여줄 표시 순서
    public static readonly AnimalType[] Order =
    {
        AnimalType.Cat, AnimalType.Dog, AnimalType.Bird, AnimalType.Rabbit, AnimalType.Squirrel,
        AnimalType.Ghost, AnimalType.Kaiju, AnimalType.Robot
    };

    public static string Name(AnimalType t) => t switch
    {
        AnimalType.Cat => "고양이",
        AnimalType.Dog => "강아지",
        AnimalType.Bird => "새",
        AnimalType.Rabbit => "토끼",
        AnimalType.Squirrel => "다람쥐",
        AnimalType.Ghost => "유령",
        AnimalType.Kaiju => "괴수",
        AnimalType.Robot => "로봇",
        _ => "기타"
    };
}
