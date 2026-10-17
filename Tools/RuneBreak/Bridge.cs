using RuneBreak.Editor;
public static class RuneBridge
{
    public static void Rules(){RuneValidation.Rules();RuneValidation.Prefabs();}
    public static void RuntimeStart()=>RuneValidation.RuntimeStart();
    public static void RuntimeVolley()=>RuneValidation.RuntimeVolley();
    public static void RuntimePresentationReset()=>RuneValidation.RuntimePresentationReset();
    public static void RuntimeAutoSolve()=>RuneValidation.RuntimeAutoSolve();
}
