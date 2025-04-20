"use client"
import { Bar } from 'react-chartjs-2';
import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend } from 'chart.js';
import ChartDataLabels, { Context } from 'chartjs-plugin-datalabels';

ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend, ChartDataLabels);

export interface ComparedData {
  percentSource: number;
  percentPostgreSql1: number;
  listRecordNotSyncPostgreSql1: unknown[];
  percentPostgreSql2: number;
  listRecordNotSyncPostgreSql2: unknown[];
  percentMySql1: number;
  listRecordNotSyncMySql1: unknown[];
  percentMySql2: number;
  listRecordNotSyncMySql2: unknown[];
}

interface ComparisonChartProps {
  data: ComparedData;
}

const ComparisonChart: React.FC<ComparisonChartProps> = ({ data }) => {
  // const maxRecordNotSync = Math.max(
  //   data.listRecordNotSyncPostgreSql1.length,
  //   data.listRecordNotSyncPostgreSql2.length,
  //   data.listRecordNotSyncMySql1.length,
  //   data.listRecordNotSyncMySql2.length
  // );
  const chartData = {
    labels: ['Database gốc', 'PostgreSQL 1(G-1, 2-Consumer)', 'PostgreSQL 2(G-2, 1-Consumer)', 'MySQL 1(G-1, 2-Consumer)', 'MySQL 2(G-2, 1-Consumer)'],
    datasets: [
      {
        label: 'Phần trăm đồng bộ dữ liệu',
        data: [
          data.percentSource,
          data.percentPostgreSql1,
          data.percentPostgreSql2,
          data.percentMySql1,
          data.percentMySql2,
        ],
        backgroundColor: ['#2980b9', '#4caf50', '#2196f3', '#ff9800', '#f44336'],
      },
      {
        label: 'Số lượng bản ghi chưa đồng bộ',
        data: [
          0, // Assuming Source doesn't have unsynced records
          data.listRecordNotSyncPostgreSql1.length,
          data.listRecordNotSyncPostgreSql2.length,
          data.listRecordNotSyncMySql1.length,
          data.listRecordNotSyncMySql2.length,
        ],
        backgroundColor: ['#95a5a6', '#8e44ad', '#34495e', '#e67e22', '#c0392b'],
      },
    ],
  };

  const options = {
    responsive: true,
    plugins: {
      legend: {
        position: 'top' as const,
      },
      title: {
        display: true,
        text: 'Kết quả đồng bộ dữ liệu',
      },
      datalabels: {
        display: true,
        color: '#fff',
        font: {
          weight: 'bold' as 'bold' | 'normal' | 'bolder' | 'lighter' | number,
        },
        formatter: (value: number, context: Context) => {
          // Check the dataset label
          if (context.dataset.label === 'Số lượng bản ghi chưa đồng bộ') {
            return value; // Display the raw number without percentage
          }
          return `${value.toFixed(3)}%`; // Display percentage for other datasets
        },
      },
    },
  };

  return <Bar data={chartData} options={options} />;
};

export default ComparisonChart;