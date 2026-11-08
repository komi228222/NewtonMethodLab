using System;

namespace NewtonMethodLab {
    public class NewtonResult {
        public double Root { get; set; }
        public double FunctionValue { get; set; }
        public double FirstDerivative { get; set; }
        public double SecondDerivative { get; set; }
        public int Iterations { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsMinimum { get; set; }
    }

    public static class NewtonMethodSolver {
        public static double FirstDerivative(Func<double, double> f, double x) {
            double h = 1e-5;
            return (f(x + h) - f(x - h)) / (2 * h);
        }

        public static double SecondDerivative(Func<double, double> f, double x) {
            double h = 1e-4;
            return (f(x + h) - 2 * f(x) + f(x - h)) / (h * h);
        }

        public static NewtonResult FindExtremum(Func<double, double> f, double startX, double epsilon, int maxIterations = 1000) {
            var result = new NewtonResult();
            double x = startX;
            int iter = 0;

            while (iter < maxIterations) {
                double df = FirstDerivative(f, x);
                double d2f = SecondDerivative(f, x);

                if (Math.Abs(d2f) < 1e-10) {
                    result.Success = false;
                    result.ErrorMessage = "Вторая производная близка к нулю. Метод не может двигаться дальше.";
                    return result;
                }

                double nextX = x - df / d2f;

                if (double.IsNaN(nextX) || double.IsInfinity(nextX)) {
                    result.Success = false;
                    result.ErrorMessage = "Метод Ньютона разошелся (ушел в бесконечность).";
                    return result;
                }

                if (Math.Abs(nextX - x) < epsilon) {
                    x = nextX;
                    iter++;
                    break;
                }

                x = nextX;
                iter++;
            }

            double finalDF = FirstDerivative(f, x);
            double finalD2F = SecondDerivative(f, x);

            result.Root = x;
            result.FunctionValue = f(x);
            result.FirstDerivative = finalDF;
            result.SecondDerivative = finalD2F;
            result.Iterations = iter;
            result.IsMinimum = finalD2F > 0;
            result.Success = true;
            return result;
        }
    }
}