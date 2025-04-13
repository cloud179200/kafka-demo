"use client";
import { useEffect, useState } from "react";
import axios from "axios";
import ComparisonChart, { ComparedData } from "../components/ComparisonChart";

export default function Home() {
  const [data, setData] = useState<ComparedData | null>(null);
  const [error, setError] = useState<string>("");

  useEffect(() => {
    const fetchData = async () => {
      try {
        const response = await axios.get(
          "http://localhost:5000/WeatherForecast/compared-data"
        );
        setData(response.data);
      } catch (err) {
        setError("Không thể lấy dữ liệu.");
        console.error(err);
      }
    };

    fetchData();
  }, []);

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col items-center justify-center p-4">
      {error ? (
        <div className="text-red-500 text-lg font-medium bg-red-50 p-4 rounded-lg">
          {error}
        </div>
      ) : !data ? (
        <div className="flex items-center gap-2 text-gray-600">
          <svg
            className="animate-spin h-5 w-5 text-blue-500"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
            ></circle>
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
            ></path>
          </svg>
          Đang tải...
        </div>
      ) : (
        <div className="w-full max-w-6xl bg-white shadow-lg rounded-lg p-6">
          <h1 className="text-2xl font-bold text-gray-800 mb-6 text-center">
            So sánh đồng bộ dữ liệu
          </h1>
          <ComparisonChart data={data} />
        </div>
      )}
    </div>
  );
}