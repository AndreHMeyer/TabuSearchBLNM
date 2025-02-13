using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace TabuSearchBLNM
{
    public class BuscaTabu
    {
        public List<List<int>> maquinas { get; set; }
        public int makespan { get; set; } // Tempo máximo de processamento
        private static Random random = new Random();

        public void IniciarBuscaTabu()
        {
            int[] quantidadeMaquinas = { 10, 20, 50 };
            double[] fatoresR = { 1.5, 2.0 };
            double[] valoresAlpha = { 0.01, 0.02, 0.03, 0.04, 0.05, 0.06, 0.07, 0.08, 0.09 };
            int repeticoes = 10;

            var log = new StringBuilder();
            log.AppendLine("heuristica;n;m;replicacao;tempo;iteracoes;valor;parametro");

            foreach (var maquina in quantidadeMaquinas)
            {
                foreach (var fatorR in fatoresR)
                {
                    int tarefas = (int)Math.Pow(maquina, fatorR);
                    var tempoTarefas = GerarTempoTarefas(tarefas);

                    for (int rep = 1; rep <= repeticoes; rep++)
                    {
                        var alphas = valoresAlpha.Concat(new double[] { random.NextDouble() * 0.09 + 0.01 }).ToArray();

                        int tabuSize = (int)(alphas[rep - 1] * tarefas);
                        var stopwatch = Stopwatch.StartNew();
                        var (melhorSolucao, iteracoesExecutadas) = ExecutarBuscaTabu(tarefas, maquina, tempoTarefas, tabuSize);
                        stopwatch.Stop();

                        log.AppendLine($"buscatabu;{tarefas};{maquina};{rep};{stopwatch.Elapsed.TotalSeconds:F2};{iteracoesExecutadas};{melhorSolucao.makespan};{alphas[rep - 1]:F2}");
                    }
                }
            }

            File.WriteAllText(@"C:\Users\AndréMeyer\Downloads\BuscaTabu500.csv", log.ToString(), Encoding.UTF8);
        }

        private List<int> GerarTempoTarefas(int tarefas)
        {
            return Enumerable.Range(0, tarefas).Select(_ => random.Next(1, 101)).ToList();
        }

        private (BuscaTabu, int) ExecutarBuscaTabu(int tarefas, int maquina, List<int> tempoTarefas, int tabuSize)
        {
            int iteracoesSemMelhora = 0, maxIteracoesSemMelhora = 1000, iteracoesExecutadas = 0, iteracoesRelizadas = 0;
            Queue<(int, int)> listaTabu = new Queue<(int, int)>();

            var melhorSolucao = GerarDistribuicaoInicial(tarefas, maquina, tempoTarefas);
            var solucaoAtual = melhorSolucao;

            while (iteracoesSemMelhora < maxIteracoesSemMelhora)
            {
                iteracoesExecutadas++;

                (BuscaTabu melhorVizinho, (int, int) melhorMovimento) = GerarMelhorVizinho(solucaoAtual, tempoTarefas, maquina, listaTabu);
                if (melhorVizinho == null) break;

                solucaoAtual = melhorVizinho;

                double novoAlpha = random.NextDouble() * 0.09 + 0.01;
                tabuSize = (int)(novoAlpha * tarefas);

                listaTabu.Enqueue(melhorMovimento);
                if (listaTabu.Count > tabuSize) listaTabu.Dequeue();

                if (solucaoAtual.makespan < melhorSolucao.makespan)
                {
                    iteracoesRelizadas++;

                    melhorSolucao = solucaoAtual;
                    iteracoesSemMelhora = 0;
                }
                else
                {
                    iteracoesSemMelhora++;
                }
            }

            return (melhorSolucao, iteracoesRelizadas);
        }

        private BuscaTabu GerarDistribuicaoInicial(int tarefas, int maquinas, List<int> tempoTarefas)
        {
            List<List<(int tarefa, int tempo)>> alocacaoMaquinas = Enumerable.Range(0, maquinas).Select(_ => new List<(int, int)>()).ToList();

            var tarefasOrdenadas = Enumerable.Range(0, tarefas).OrderByDescending(t => tempoTarefas[t]).ToList();

            foreach (int tarefa in tarefasOrdenadas)
            {
                int indiceAleatorio = random.Next(maquinas);
                alocacaoMaquinas[indiceAleatorio].Add((tarefa, tempoTarefas[tarefa]));
            }

            // Filtra apenas com índices das tarefas
            var maquinasApenasTarefas = alocacaoMaquinas.Select(lista => lista.Select(x => x.tarefa).ToList()).ToList();

            return new BuscaTabu { maquinas = maquinasApenasTarefas, makespan = CalcularMakespan(maquinasApenasTarefas, tempoTarefas) };
        }


        private (BuscaTabu, (int, int)) GerarMelhorVizinho(BuscaTabu solucaoAtual, List<int> tempoTarefas, int m, Queue<(int, int)> listaTabu)
        {
            BuscaTabu melhorVizinho = null;
            (int, int) melhorMovimento = (-1, -1);

            foreach (var (origem, destino) in GerarMovimentos(m))
            {
                if (solucaoAtual.maquinas[origem].Count == 0 || listaTabu.Contains((origem, destino))) continue;

                BuscaTabu novaSolucao = AplicarMovimento(solucaoAtual, origem, destino, tempoTarefas);

                if (melhorVizinho == null || novaSolucao.makespan < melhorVizinho.makespan)
                {
                    melhorVizinho = novaSolucao;
                    melhorMovimento = (origem, destino);
                }
            }

            return (melhorVizinho, melhorMovimento);
        }

        private List<(int, int)> GerarMovimentos(int maquinas)
        {
            return Enumerable.Range(0, maquinas).SelectMany(i => Enumerable.Range(0, maquinas).Where(j => i != j), (i, j) => (i, j)).ToList();
        }

        private BuscaTabu AplicarMovimento(BuscaTabu solucao, int origem, int destino, List<int> tempoTarefas)
        {
            if (solucao.maquinas[origem].Count == 0) return solucao;

            int tarefa = solucao.maquinas[origem].OrderByDescending(t => tempoTarefas[t]).First();
            List<List<int>> novasMaquinas = solucao.maquinas.Select(m => new List<int>(m)).ToList();

            novasMaquinas[origem].Remove(tarefa);
            novasMaquinas[destino].Add(tarefa);

            return new BuscaTabu { maquinas = novasMaquinas, makespan = CalcularMakespan(novasMaquinas, tempoTarefas) };
        }

        private int CalcularMakespan(List<List<int>> maquinas, List<int> tempoTarefas)
        {
            return maquinas.Max(maquina => maquina.Sum(tarefa => tempoTarefas[tarefa]));
        }
    }
}
