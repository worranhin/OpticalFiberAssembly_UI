namespace OpticalFiberAssembly
{
    internal class StepperStatus
    {
        public int position { get; set; }
        public float speed { get; set; }
        public float acceleration { get; set; }
        public int target { get; set; }
    }
}