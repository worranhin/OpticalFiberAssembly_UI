namespace OpticalFiberAssembly
{
    internal class StepperStatus
    {
        public int position { get; set; }
        public int speed { get; set; }
        public int acceleration { get; set; }
        public int target { get; set; }
    }
}