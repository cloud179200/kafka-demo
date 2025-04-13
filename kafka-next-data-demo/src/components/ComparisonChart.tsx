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
  console.log({ data })
  const chartData = {
    labels: ['Nguồn', 'PostgreSQL 1(Cùng group, 2 instance consumer)', 'PostgreSQL 2(1 group, 1 instance consumer)', 'MySQL 1(Cùng group, 2 instance consumer)', 'MySQL 2(1 group, 1 instance consumer)'],
    datasets: [
      {
        label: 'Phần trăm đồng bộ dữ liệu',
        data: [
          data.percentSource / 10,
          data.percentPostgreSql1 / 10,
          data.percentPostgreSql2 / 10,
          data.percentMySql1 / 10,
          data.percentMySql2 / 10,
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
        text: 'So sánh đồng bộ dữ liệu',
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