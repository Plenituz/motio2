namespace Motio.Animation
{
    /// <summary>
    /// this has to be implemented by the Nodeproperty of different types and passed 
    /// to the KeyframeHolder
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IInterpolator
    {
        float InterpolateBetween(KeyframeFloat t1, KeyframeFloat t2, int time);
    }
}
